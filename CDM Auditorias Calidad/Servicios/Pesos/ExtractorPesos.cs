using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace CDM_Auditorias_Calidad.Servicios.Pesos;

/// <summary>Un KPI de un bloque de objetivos: su peso (fracción) y sus metas, en el orden de la cabecera del bloque.</summary>
public sealed record FilaPeso(string Kpi, double? Peso, List<double?> Metas);

/// <summary>Un bloque de objetivos de una hoja: «Objetivos CO Atención | (Peso) | 0 | 1 | 1,5» y una fila por KPI.</summary>
/// <param name="Cabecera">Los porcentajes de cumplimiento de cada meta (0, 0,6, 1, 1,5).</param>
public sealed record BloquePesos(string Hoja, string Nivel, string? Titulo, List<double> Cabecera, List<FilaPeso> Filas)
{
    public double SumaPesos => Filas.Sum(f => f.Peso ?? 0);
}

/// <summary>
/// Saca los bloques de objetivos de las primeras filas de una hoja. Cada Excel lo pone en un sitio y con un rótulo
/// distinto («Peso», «Ponderación», nada…), así que no se busca el rótulo sino la cabecera de metas: tres o más cifras
/// seguidas que empiezan en 0, incluyen el 1 y crecen hasta como mucho 2 (0 | 1 | 1,5 o 0 | 0,6 | 1 | 1,5). La columna de
/// su izquierda es el peso y la siguiente, el nombre del KPI; debajo, una fila por KPI hasta la primera sin nombre.
/// </summary>
public static class ExtractorPesos
{
    public static List<BloquePesos> Extraer(CabeceraHoja hoja)
    {
        var g = hoja.Celdas;
        var salida = new List<BloquePesos>();
        for (var r = 0; r < g.Length; r++)
        {
            var f = g[r];
            for (var c = 2; c < f.Length - 2; c++)
            {
                var metas = new List<double>();
                var k = c;
                while (k < f.Length && metas.Count < 5 && Numero(f[k]) is { } v) { metas.Add(v); k++; }
                if (!EsCabeceraDeMetas(metas)) continue;

                var colPeso = c - 1;
                // El nombre del KPI: la columna con texto más cercana a la izquierda del peso en la primera fila (WhatsApp
                // Técnico deja una columna vacía en medio). Sin texto en ninguna, no es un bloque.
                var colKpi = -1;
                if (r + 1 < g.Length)
                {
                    for (var x = c - 2; x >= Math.Max(0, c - 4); x--)
                    {
                        if (g[r + 1][x] is string t && !string.IsNullOrWhiteSpace(t) && Numero(t) is null) { colKpi = x; break; }
                    }
                }
                if (colKpi < 0) continue;

                var filas = new List<(FilaPeso Fila, string? Grupo)>();
                for (var rr = r + 1; rr < g.Length && rr < r + 25; rr++)
                {
                    if (g[rr][colKpi] is not string nombre || string.IsNullOrWhiteSpace(nombre)) break;
                    // Otro bloque apilado justo debajo (Técnico Orange: uno por skill): aquí acaba este.
                    if (EsCabeceraDeMetas(Enumerable.Range(c, metas.Count).Select(j => j < g[rr].Length ? Numero(g[rr][j]) : null)
                            .TakeWhile(x => x is not null).Select(x => x!.Value).ToList())) break;
                    var grupo = colKpi > 0 && g[rr][colKpi - 1] is string gr && !string.IsNullOrWhiteSpace(gr) ? Limpio(gr) : null;
                    filas.Add((new FilaPeso(Limpio(nombre), Numero(g[rr][colPeso]),
                        Enumerable.Range(0, metas.Count).Select(j => c + j < g[rr].Length ? Numero(g[rr][c + j]) : null).ToList()), grupo));
                }
                if (filas.Count > 0)
                {
                    var titulo = g[r][colKpi] as string ?? g[r][colPeso] as string;
                    foreach (var (grupo, parte) in PorGrupos(filas))
                    {
                        var t = grupo ?? titulo;
                        salida.Add(new BloquePesos(hoja.Hoja.Trim(), Nivel(hoja.Hoja), t is null ? null : Limpio(t), metas, parte));
                    }
                }
                c = k;
            }
        }
        return salida;
    }

    /// <summary>
    /// Parte un bloque por grupos cuando la columna de la izquierda del KPI trae un rótulo de vez en cuando (WhatsApp Técnico:
    /// «WhatsApp» en la primera fila y «Técnico» más abajo). Solo si la primera fila lo trae y no lo trae (casi) cada fila: si no,
    /// esa columna es otra cosa y el bloque va entero.
    /// </summary>
    private static IEnumerable<(string? Grupo, List<FilaPeso> Filas)> PorGrupos(List<(FilaPeso Fila, string? Grupo)> filas)
    {
        var conGrupo = filas.Count(f => f.Grupo is not null);
        if (filas[0].Grupo is null || conGrupo < 2 || conGrupo * 2 > filas.Count)
        {
            yield return (null, filas.Select(f => f.Fila).ToList());
            yield break;
        }
        string? actual = null;
        var parte = new List<FilaPeso>();
        foreach (var (fila, grupo) in filas)
        {
            if (grupo is not null && parte.Count > 0)
            {
                yield return (actual, parte);
                parte = new List<FilaPeso>();
            }
            if (grupo is not null) actual = grupo;
            parte.Add(fila);
        }
        if (parte.Count > 0) yield return (actual, parte);
    }

    /// <summary>Los nombres de la hoja principal de agentes, en orden de preferencia (pedido del usuario el 07-10-2026).</summary>
    public static readonly string[] HojasPrincipales = ["ranking ag universal", "ranking ag", "ranking agente"];

    /// <summary>
    /// La hoja principal de agentes de un fichero: la primera que se llame como <see cref="HojasPrincipales"/> (sin mirar
    /// mayúsculas, tildes ni espacios de más) y, si no hay ninguna, la primera hoja de agentes del libro («CO Atención YGMM»,
    /// «Senior»). Nulo si el fichero no tiene bloques de agentes. Así cada sector sale una sola vez, sin «Mes 1», «Mes 2», «TLT»…
    /// </summary>
    public static string? HojaPrincipal(IEnumerable<BloquePesos> bloques)
    {
        var deAgentes = bloques.Where(b => b.Nivel == "Agente").Select(b => b.Hoja).Distinct().ToList();
        foreach (var nombre in HojasPrincipales)
        {
            var hoja = deAgentes.FirstOrDefault(h => Normal(h) == nombre);
            if (hoja is not null) return hoja;
        }
        return deAgentes.FirstOrDefault();
    }

    /// <summary>Si dos nombres de hoja son la misma, sin mirar mayúsculas, tildes ni espacios de más.</summary>
    public static bool MismaHoja(string a, string b) => Normal(a) == Normal(b);

    private static string Normal(string s) => Regex.Replace(SinTildes(s).ToLowerInvariant(), @"\s+", " ").Trim();

    /// <summary>0 | 1 | 1,5 o 0 | 0,6 | 1 | 1,5: empieza en 0, incluye el 1, crece y no pasa de 2.</summary>
    public static bool EsCabeceraDeMetas(List<double> v)
        => v.Count >= 3 && v[0] == 0 && v.Contains(1) && v.Zip(v.Skip(1)).All(p => p.Second > p.First) && v[^1] <= 2;

    /// <summary>
    /// Nivel de una hoja por su nombre: «JS» o «Jefe» → Jefe de servicio; «SP» o «Super» → Supervisor; «TL» o «Team» → Team
    /// leader; lo demás, Agente. Por palabras enteras: «Ranking Agentes TLT» es de agentes.
    /// </summary>
    public static string Nivel(string hoja)
    {
        var palabras = Regex.Split(SinTildes(hoja).ToLowerInvariant(), @"[^a-z0-9]+").Where(p => p.Length > 0).ToHashSet();
        if (palabras.Overlaps(["js", "jefe", "jds"])) return "Jefe de servicio";
        if (palabras.Overlaps(["sp", "super", "supervisor", "supervisores"])) return "Supervisor";
        if (palabras.Overlaps(["tl", "team", "coordinador", "coordinadores"])) return "Team leader";
        return "Agente";
    }

    /// <summary>Un número, también si viene como texto («0», «1,5», «22,5%»).</summary>
    public static double? Numero(object? x) => x switch
    {
        double d => d,
        string s when TextoANumero(s) is { } d => d,
        _ => null,
    };

    private static double? TextoANumero(string s)
    {
        var t = s.Trim().Replace(" ", "");
        var porcentaje = t.EndsWith('%');
        t = t.TrimEnd('%').Trim().Replace(',', '.');
        if (t.Length == 0 || !double.TryParse(t, NumberStyles.Float, CultureInfo.InvariantCulture, out var d)) return null;
        return porcentaje ? d / 100 : d;
    }

    private static string Limpio(string s) => Regex.Replace(s.Replace(" ", " "), @"\s+", " ").Trim();

    public static string SinTildes(string s)
    {
        var sb = new StringBuilder();
        foreach (var ch in s.Normalize(NormalizationForm.FormD))
        {
            if (CharUnicodeInfo.GetUnicodeCategory(ch) != UnicodeCategory.NonSpacingMark) sb.Append(ch);
        }
        return sb.ToString().Normalize(NormalizationForm.FormC);
    }
}
