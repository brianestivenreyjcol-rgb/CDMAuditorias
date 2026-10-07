using System.Globalization;
using CDM_Auditorias_Calidad.Servicios.Configuracion;
using CDM_Auditorias_Calidad.Servicios.NoSolucion;
using Microsoft.Data.SqlClient;

namespace CDM_Auditorias_Calidad.Servicios.Sectores;

/// <summary>
/// Trae las tres consultas de <c>Consultas/</c>: <c>SectoresYgmm.sql</c> (BigQuery, los dos indicadores de YGMM),
/// <c>SectoresRellamada.sql</c> y <c>SectoresNoSolucion.sql</c> (SQL Server, Jazztel, Orange y WhatsApp).
/// </summary>
/// <remarks>
/// Las consultas se leen en cada carga: editar el <c>.sql</c> y pulsar «Actualizar ahora» basta. Si una fuente falla,
/// las demás se enseñan igual y el fallo va a <see cref="DatosSectores.Avisos"/>; si fallan todas, es un error.
/// BigQuery pasa por el mismo cliente ODBC que No solución y GAIA (un único turno con el driver).
/// </remarks>
public sealed class FuenteSectores
{
    private readonly string _odbc;
    private readonly string _carpetaConsultas;
    private readonly Func<DatosConexion> _conexion;
    private readonly int _segundosConsulta;
    private readonly ILogger? _log;

    public FuenteSectores(string odbc, string carpetaConsultas, Func<DatosConexion> conexion, int segundosConsulta, ILogger? log = null)
    {
        _odbc = odbc;
        _carpetaConsultas = carpetaConsultas;
        _conexion = conexion;
        _segundosConsulta = segundosConsulta;
        _log = log;
    }

    public async Task<DatosSectores> TraerAsync(CancellationToken ct)
    {
        var filas = new List<FilaSector>();
        var avisos = new List<string>();
        var fallos = 0;

        async Task Intentar(string que, Func<Task<IEnumerable<FilaSector>>> traer)
        {
            try
            {
                filas.AddRange(await traer());
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                fallos++;
                avisos.Add($"No se pudo traer {que}: {ex.Message}");
                _log?.LogError(ex, "Sectores: falló {Que}", que);
            }
        }

        await Intentar("YGMM de BigQuery", () => TraerYgmmAsync(ct));
        await Intentar("la retención de YGMM de BigQuery", () => TraerBigQueryAsync("SectoresRetencion.sql", IndicadorSectores.Retencion.Clave, ct));
        await Intentar("la rellamada de SQL Server (Jazztel, Orange y WhatsApp)",
            () => TraerSqlAsync("SectoresRellamada.sql", IndicadorSectores.Rellamada.Clave, ct));
        await Intentar("la no solución de SQL Server (Jazztel y Orange)",
            () => TraerSqlAsync("SectoresNoSolucion.sql", IndicadorSectores.NoSolucion.Clave, ct));

        if (fallos == 4) throw new InvalidOperationException(string.Join(" · ", avisos));

        return new DatosSectores { Filas = Juntar(filas), Avisos = avisos, Generado = DateTime.Now };
    }

    /// <summary>Junta las filas repetidas (mismo indicador, mes, sector y marca) y quita las vacías.</summary>
    public static List<FilaSector> Juntar(IEnumerable<FilaSector> filas)
        => filas.Where(f => f.Base > 0)
            .GroupBy(f => (f.Indicador, f.Mes, f.Sector, f.Marca))
            .Select(g => g.First() with { Base = g.Sum(f => f.Base), Casos = g.Sum(f => f.Casos) })
            .OrderBy(f => f.Indicador).ThenBy(f => f.Sector, StringComparer.CurrentCulture).ThenBy(f => f.Mes, StringComparer.Ordinal)
            .ToList();

    private string Consulta(string fichero) => File.ReadAllText(Path.Combine(_carpetaConsultas, fichero));

    private async Task<IEnumerable<FilaSector>> TraerYgmmAsync(CancellationToken ct)
    {
        var filas = await FuenteBigQuery.ConsultarAsync(_odbc, Consulta("SectoresYgmm.sql"), _log, ct);
        var salida = new List<FilaSector>();
        foreach (var r in filas)
        {
            var mes = Texto(r, "Mes");
            var sector = Sector(Texto(r, "Sector"));
            var marca = Texto(r, "Marca");
            salida.Add(new FilaSector(IndicadorSectores.Rellamada.Clave, mes, sector, marca, "BigQuery", Numero(r, "BaseRellamada"), Numero(r, "CasosRellamada")));
            salida.Add(new FilaSector(IndicadorSectores.NoSolucion.Clave, mes, sector, marca, "BigQuery", Numero(r, "BaseNoSolucion"), Numero(r, "CasosNoSolucion")));
        }
        return salida;
    }

    /// <summary>Una consulta de BigQuery que ya devuelve Mes, Sector, Marca, Base y Casos de un indicador.</summary>
    private async Task<IEnumerable<FilaSector>> TraerBigQueryAsync(string fichero, string indicador, CancellationToken ct)
    {
        var filas = await FuenteBigQuery.ConsultarAsync(_odbc, Consulta(fichero), _log, ct);
        return filas.Select(r => new FilaSector(indicador, Texto(r, "Mes"), Sector(Texto(r, "Sector")), Texto(r, "Marca"), "BigQuery",
            Numero(r, "Base"), Numero(r, "Casos"))).ToList();
    }

    private async Task<IEnumerable<FilaSector>> TraerSqlAsync(string fichero, string indicador, CancellationToken ct)
    {
        var sql = Consulta(fichero);
        var salida = new List<FilaSector>();
        await using var cn = new SqlConnection(_conexion().CadenaDeConexion);
        await cn.OpenAsync(ct);
        await using var cmd = new SqlCommand(sql, cn) { CommandTimeout = _segundosConsulta };
        await using var rd = await cmd.ExecuteReaderAsync(ct);
        int iMes = rd.GetOrdinal("Mes"), iSector = rd.GetOrdinal("Sector"), iMarca = rd.GetOrdinal("Marca"),
            iBase = rd.GetOrdinal("Base"), iCasos = rd.GetOrdinal("Casos");
        while (await rd.ReadAsync(ct))
        {
            if (rd.IsDBNull(iMes)) continue;
            salida.Add(new FilaSector(indicador, rd.GetString(iMes), Sector(rd.IsDBNull(iSector) ? "" : rd.GetString(iSector)),
                rd.GetString(iMarca), "SQL Server",
                rd.IsDBNull(iBase) ? 0 : Convert.ToInt64(rd.GetValue(iBase), CultureInfo.InvariantCulture),
                rd.IsDBNull(iCasos) ? 0 : Convert.ToInt64(rd.GetValue(iCasos), CultureInfo.InvariantCulture)));
        }
        return salida;
    }

    private static string Sector(string s) => string.IsNullOrWhiteSpace(s) ? "Sin sector" : s.Trim();

    private static string Texto(Dictionary<string, object?> fila, string clave)
        => fila.TryGetValue(clave, out var v) && v is not null ? Convert.ToString(v, CultureInfo.InvariantCulture) ?? "" : "";

    private static long Numero(Dictionary<string, object?> fila, string clave)
        => fila.TryGetValue(clave, out var v) && v is not null ? Convert.ToInt64(v, CultureInfo.InvariantCulture) : 0;
}
