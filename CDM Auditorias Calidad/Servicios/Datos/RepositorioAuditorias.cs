using System.Diagnostics;
using System.Globalization;
using CDM_Auditorias_Calidad.Models;
using CDM_Auditorias_Calidad.Servicios.Configuracion;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;

namespace CDM_Auditorias_Calidad.Servicios.Datos;

/// <summary>
/// Ejecuta la consulta del Power BI (<c>Consultas/Auditorias.sql</c>) y devuelve sus filas.
/// </summary>
public sealed class RepositorioAuditorias
{
    private readonly IOptions<OpcionesAuditorias> _opciones;
    private readonly IWebHostEnvironment _entorno;

    public RepositorioAuditorias(IOptions<OpcionesAuditorias> opciones, IWebHostEnvironment entorno)
    {
        _opciones = opciones;
        _entorno = entorno;
    }

    // Las consultas se leen de la carpeta de la aplicación (en desarrollo, la del proyecto; en
    // producción, la publicada) y en cada carga: un cambio en el .sql vale con pulsar
    // «Actualizar», sin recompilar. Antes se leía la copia de bin, que no siempre se refrescaba
    // al compilar (02-10-2026: el «- 3» de ICEBERG no llegaba a la web).
    public string RutaConsulta => Path.Combine(_entorno.ContentRootPath, "Consultas", "Auditorias.sql");

    public async Task<InstantaneaAuditorias> CargarAsync(CancellationToken ct)
    {
        var op = _opciones.Value;
        // El .env se lee en cada carga: así un cambio de contraseña no obliga a reiniciar.
        var conexion = DatosConexion.Cargar(Path.Combine(_entorno.ContentRootPath, op.RutaEnv));
        var sql = await File.ReadAllTextAsync(RutaConsulta, ct);

        var reloj = Stopwatch.StartNew();
        var filas = new List<Auditoria>(20_000);

        await using var cn = new SqlConnection(conexion.CadenaDeConexion);
        await cn.OpenAsync(ct);
        await using var cmd = new SqlCommand(sql, cn) { CommandTimeout = op.SegundosConsulta };
        await using var rd = await cmd.ExecuteReaderAsync(ct);

        int iFecha = rd.GetOrdinal("Fecha"), iLegajo = rd.GetOrdinal("legajo"), iSector = rd.GetOrdinal("Sector"),
            iSuper = rd.GetOrdinal("Super"), iTeam = rd.GetOrdinal("Team"), iAgente = rd.GetOrdinal("Agente"),
            iLlamada = rd.GetOrdinal("ID_Llamada"), iCorreo = rd.GetOrdinal("CorreoAuditor"),
            iAuditor = rd.GetOrdinal("Nombre_Auditor"), iCargo = rd.GetOrdinal("Cargo_Auditor"),
            iRespuesta = rd.GetOrdinal("Respuesta"), iBase = rd.GetOrdinal("Base");

        while (await rd.ReadAsync(ct))
        {
            // Sin fecha la fila no entra en ningún filtro ni gráfico (en el PBI queda fuera del calendario).
            if (rd.IsDBNull(iFecha)) continue;

            filas.Add(new Auditoria(
                DateOnly.FromDateTime(rd.GetDateTime(iFecha)),
                Legajo(rd, iLegajo),
                Texto(rd, iSector),
                Texto(rd, iSuper),
                Texto(rd, iTeam),
                Texto(rd, iAgente),
                Texto(rd, iLlamada),
                Texto(rd, iCorreo),
                Texto(rd, iAuditor),
                Texto(rd, iCargo),
                rd.IsDBNull(iRespuesta) ? null : Convert.ToDouble(rd.GetValue(iRespuesta), CultureInfo.InvariantCulture),
                Texto(rd, iBase)));
        }

        await rd.CloseAsync();

        // La nómina va aparte: si falla, el informe sigue con las auditorías y la tarjeta
        // «Total agentes» sale sin dato.
        IReadOnlyList<RegistroNomina>? nomina = null;
        string? errorNomina = null;
        if (filas.Count > 0)
        {
            try
            {
                nomina = await CargarNominaAsync(cn, filas.Min(f => f.Fecha), filas.Max(f => f.Fecha), op.SegundosConsulta, ct);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                errorNomina = ex.Message;
            }
        }

        return new InstantaneaAuditorias(filas, DateTime.Now, reloj.Elapsed, nomina, errorNomina);
    }

    public string RutaConsultaNomina => Path.Combine(_entorno.ContentRootPath, "Consultas", "Nomina.sql");

    private async Task<IReadOnlyList<RegistroNomina>> CargarNominaAsync(SqlConnection cn, DateOnly desde, DateOnly hasta, int segundos, CancellationToken ct)
    {
        var sql = await File.ReadAllTextAsync(RutaConsultaNomina, ct);
        await using var cmd = new SqlCommand(sql, cn) { CommandTimeout = segundos };
        cmd.Parameters.Add("@Desde", System.Data.SqlDbType.Date).Value = desde.ToDateTime(TimeOnly.MinValue);
        cmd.Parameters.Add("@Hasta", System.Data.SqlDbType.Date).Value = hasta.ToDateTime(TimeOnly.MinValue);
        await using var rd = await cmd.ExecuteReaderAsync(ct);

        int iFecha = rd.GetOrdinal("Fecha"), iLegajo = rd.GetOrdinal("legajo"), iSector = rd.GetOrdinal("Sector"),
            iSuper = rd.GetOrdinal("Super"), iTeam = rd.GetOrdinal("Team"), iCargo = rd.GetOrdinal("Cargo");

        // Unas 200.000 filas con muy pocos textos distintos: se comparten las cadenas.
        var textos = new Dictionary<string, string>(StringComparer.Ordinal);
        string? Compartido(SqlDataReader r, int i)
        {
            var s = Texto(r, i)?.Trim();
            if (s is null) return null;
            if (!textos.TryGetValue(s, out var c)) textos[s] = c = s;
            return c;
        }

        var nomina = new List<RegistroNomina>(250_000);
        while (await rd.ReadAsync(ct))
        {
            if (rd.IsDBNull(iFecha) || Legajo(rd, iLegajo) is not { } legajo) continue;
            nomina.Add(new RegistroNomina(
                DateOnly.FromDateTime(rd.GetDateTime(iFecha)),
                legajo,
                Compartido(rd, iSector),
                Compartido(rd, iSuper),
                Compartido(rd, iTeam),
                Compartido(rd, iCargo)));
        }
        return nomina;
    }

    private static string? Texto(SqlDataReader rd, int i)
        => rd.IsDBNull(i) ? null : Convert.ToString(rd.GetValue(i), CultureInfo.InvariantCulture);

    /// <summary>
    /// El legajo llega como número en WEB y puede llegar como texto en ICEBERG.
    /// </summary>
    private static long? Legajo(SqlDataReader rd, int i)
    {
        if (rd.IsDBNull(i)) return null;
        var v = rd.GetValue(i);
        if (v is string s)
            return long.TryParse(s.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var n) ? n : null;
        return Convert.ToInt64(v, CultureInfo.InvariantCulture);
    }
}
