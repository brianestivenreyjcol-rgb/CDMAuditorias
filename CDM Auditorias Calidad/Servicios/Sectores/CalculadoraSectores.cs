using System.Globalization;
using CDM_Auditorias_Calidad.Infraestructura;
using CDM_Auditorias_Calidad.Models;

namespace CDM_Auditorias_Calidad.Servicios.Sectores;

/// <summary>Los parámetros de la URL: marca y sector (repetibles) y el mes de referencia.</summary>
public sealed class PeticionSectores
{
    public List<string> Marca { get; set; } = new();
    public List<string> Sector { get; set; } = new();
    public string? Mes { get; set; }
}

/// <summary>Base y casos de un grupo; el porcentaje es casos ÷ base (en fracción).</summary>
public sealed record CifraSector(long Base, long Casos)
{
    public static readonly CifraSector Cero = new(0, 0);
    public double? Pct => Base > 0 ? (double)Casos / Base : null;
    public CifraSector Mas(CifraSector o) => new(Base + o.Base, Casos + o.Casos);
}

/// <summary>Un mes de la ventana. <see cref="EnCurso"/>: aún no está completo (el mes actual o, en rellamada, hasta 3 días después).</summary>
public sealed record MesSectores(string Clave, string Texto, string TextoLargo, bool EnCurso);

/// <summary>Una fila de la tabla sector × mes.</summary>
public sealed record FilaTablaSector(string Sector, string Marca, string Fuente, IReadOnlyDictionary<string, CifraSector> PorMes)
{
    public CifraSector En(string mes) => PorMes.TryGetValue(mes, out var c) ? c : CifraSector.Cero;
}

/// <summary>Todo lo que pinta una pestaña con los filtros aplicados.</summary>
public sealed class ResultadoSectores
{
    public required IndicadorSectores Indicador { get; init; }
    public required PeticionSectores Peticion { get; init; }
    public required IReadOnlyList<GrupoFiltro> Desplegables { get; init; }
    public required IReadOnlyList<MesSectores> Meses { get; init; }

    /// <summary>El mes de las tarjetas y las barras (por defecto, el último cerrado).</summary>
    public required MesSectores Mes { get; init; }
    public required bool MesPorDefecto { get; init; }
    public MesSectores? MesAnterior { get; init; }

    public required IReadOnlyDictionary<string, CifraSector> TotalPorMes { get; init; }
    public required IReadOnlyList<FilaTablaSector> Tabla { get; init; }

    /// <summary>Los sectores en el mes elegido, del porcentaje más alto al más bajo.</summary>
    public required IReadOnlyList<FilaTablaSector> PorSectorDelMes { get; init; }

    public CifraSector Total(string mes) => TotalPorMes.TryGetValue(mes, out var c) ? c : CifraSector.Cero;

    /// <summary>Diferencia en puntos (fracción) del mes elegido frente al anterior; nulo si falta alguno.</summary>
    public double? Variacion => MesAnterior is { } a && Total(Mes.Clave).Pct is { } x && Total(a.Clave).Pct is { } y ? x - y : null;

    /// <summary>Los filtros para la URL (el mes solo si no es el de por defecto).</summary>
    public IEnumerable<KeyValuePair<string, string>> Parametros(bool conMes = true)
    {
        foreach (var m in Peticion.Marca) yield return new("marca", m);
        foreach (var s in Peticion.Sector) yield return new("sector", s);
        if (conMes && !MesPorDefecto) yield return new("mes", Mes.Clave);
    }
}

/// <summary>
/// Los cálculos de «Rellamada y No solución por sector»: meses, totales, filtros en cascada y la tabla sector × mes.
/// Sin acceso a datos: recibe las filas ya traídas.
/// </summary>
public static class CalculadoraSectores
{
    /// <summary>Las marcas en el orden del filtro y su texto.</summary>
    public static readonly IReadOnlyList<(string Clave, string Texto)> Marcas =
        [("YGMM", "YOIGO · MASMOVIL"), ("JAZZTEL", "JAZZTEL"), ("ORANGE", "ORANGE")];

    public static string TextoMarca(string marca) => Marcas.FirstOrDefault(m => m.Clave == marca).Texto ?? marca;

    /// <summary>Si el mes aún no está completo: su último día más los días de cierre del indicador no han pasado antes de ayer.</summary>
    public static bool EnCurso(string mes, IndicadorSectores ind, DateOnly hoy)
    {
        if (!DateOnly.TryParseExact(mes + "-01", "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var inicio)) return false;
        var ultimo = inicio.AddMonths(1).AddDays(-1);
        // Los datos llegan hasta ayer: el mes está completo si su último día + los días de cierre es anterior a hoy.
        return ultimo.AddDays(ind.DiasCierre) >= hoy;
    }

    public static MesSectores Mes(string clave, IndicadorSectores ind, DateOnly hoy)
    {
        var d = DateOnly.ParseExact(clave + "-01", "yyyy-MM-dd", CultureInfo.InvariantCulture);
        return new MesSectores(clave, Formato.Mayuscula(Formato.MesCorto(d.Year, d.Month)) + " " + d.Year, Formato.MesLargo(d.Year, d.Month), EnCurso(clave, ind, hoy));
    }

    public static ResultadoSectores Resolver(DatosSectores datos, PeticionSectores p, IndicadorSectores ind, DateOnly hoy)
    {
        var delIndicador = datos.Filas.Where(f => f.Indicador == ind.Clave && DateOnly.TryParseExact(f.Mes + "-01", "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out _)).ToList();

        // Solo cuenta lo marcado que existe: un enlace viejo con un sector que ya no está no deja la página vacía.
        var marcasValidas = delIndicador.Select(f => f.Marca).ToHashSet();
        var sectoresValidos = delIndicador.Select(f => f.Sector).ToHashSet();
        var peticion = new PeticionSectores
        {
            Marca = p.Marca.Where(marcasValidas.Contains).Distinct().ToList(),
            Sector = p.Sector.Where(sectoresValidos.Contains).Distinct().ToList(),
            Mes = p.Mes,
        };
        bool PasaMarca(FilaSector f) => peticion.Marca.Count == 0 || peticion.Marca.Contains(f.Marca);
        bool PasaSector(FilaSector f) => peticion.Sector.Count == 0 || peticion.Sector.Contains(f.Sector);
        var filas = delIndicador.Where(f => PasaMarca(f) && PasaSector(f)).ToList();

        // Desplegables en cascada: cada uno con lo que existe con el otro filtro; la cifra es la base de toda la ventana.
        var desplegables = new List<GrupoFiltro>
        {
            new("marca", "Marca", Marcas
                .Select(m => (m.Clave, m.Texto, Base: delIndicador.Where(f => f.Marca == m.Clave && PasaSector(f)).Sum(f => f.Base)))
                .Where(m => m.Base > 0 || peticion.Marca.Contains(m.Clave))
                .Select(m => new OpcionFiltro(m.Clave, m.Texto, Recorte(m.Base), peticion.Marca.Contains(m.Clave))).ToList()),
            new("sector", "Sector", delIndicador.Where(PasaMarca).GroupBy(f => f.Sector)
                .Select(g => (Sector: g.Key, Base: g.Sum(f => f.Base)))
                .Concat(peticion.Sector.Where(s => !delIndicador.Where(PasaMarca).Any(f => f.Sector == s)).Select(s => (Sector: s, Base: 0L)))
                .OrderBy(x => x.Sector, StringComparer.CurrentCulture)
                .Select(x => new OpcionFiltro(x.Sector, x.Sector, Recorte(x.Base), peticion.Sector.Contains(x.Sector))).ToList()),
        };

        var meses = delIndicador.Select(f => f.Mes).Distinct().OrderBy(m => m, StringComparer.Ordinal).Select(m => Mes(m, ind, hoy)).ToList();
        var porDefecto = meses.LastOrDefault(m => !m.EnCurso) ?? meses.LastOrDefault() ?? Mes(hoy.ToString("yyyy-MM", CultureInfo.InvariantCulture), ind, hoy);
        var elegido = meses.FirstOrDefault(m => m.Clave == p.Mes) ?? porDefecto;
        var indice = meses.FindIndex(m => m.Clave == elegido.Clave);

        var totales = filas.GroupBy(f => f.Mes).ToDictionary(g => g.Key, g => Suma(g));
        var tabla = filas.GroupBy(f => (f.Sector, f.Marca))
            .Select(g => new FilaTablaSector(g.Key.Sector, g.Key.Marca, g.First().Fuente,
                g.GroupBy(f => f.Mes).ToDictionary(m => m.Key, m => Suma(m))))
            .OrderBy(f => OrdenMarca(f.Marca))
            .ThenByDescending(f => f.En(elegido.Clave).Base)
            .ToList();
        var delMes = tabla.Where(f => f.En(elegido.Clave).Base > 0)
            .OrderByDescending(f => f.En(elegido.Clave).Pct).ThenByDescending(f => f.En(elegido.Clave).Base).ToList();

        return new ResultadoSectores
        {
            Indicador = ind,
            Peticion = peticion,
            Desplegables = desplegables,
            Meses = meses,
            Mes = elegido,
            MesPorDefecto = elegido.Clave == porDefecto.Clave,
            MesAnterior = indice > 0 ? meses[indice - 1] : null,
            TotalPorMes = totales,
            Tabla = tabla,
            PorSectorDelMes = delMes,
        };
    }

    /// <summary>Posición de la marca en <see cref="Marcas"/> (las desconocidas, al final).</summary>
    public static int OrdenMarca(string marca)
    {
        for (var i = 0; i < Marcas.Count; i++)
        {
            if (Marcas[i].Clave == marca) return i;
        }
        return Marcas.Count;
    }

    private static CifraSector Suma(IEnumerable<FilaSector> filas)
        => new(filas.Sum(f => f.Base), filas.Sum(f => f.Casos));

    private static int Recorte(long v) => (int)Math.Min(int.MaxValue, v);
}
