using System.Globalization;

namespace CDM_Auditorias_Calidad.Servicios.Gaia;

/// <summary>Una porción de un reparto (resultado de venta, estado de resolución…). <paramref name="Fraccion"/> sobre el total del reparto.</summary>
public sealed record RepartoGaia(string Texto, int Cantidad, double Fraccion);

/// <summary>
/// Un motivo de contacto con sus indicadores y sus submotivos (Motivo 1 → 2 → 3, el «zoom» de las páginas
/// «Motivos de Contacto» y «Mapa de afectación» del PBI).
/// </summary>
/// <param name="Participacion">Peso dentro de su motivo padre (medida <c>% Participación Llamadas</c>).</param>
public sealed record NodoMotivo(string Clave, int Nivel, IndicadoresGaia Indicadores, double Participacion, IReadOnlyList<NodoMotivo> Hijos);

/// <summary>Españolización de un grupo de llamadas en un nivel. Fracciones sobre <see cref="Base"/>.</summary>
/// <param name="ConTranscripcion">Llamadas que están en la tabla de transcripciones.</param>
/// <param name="Base">Las de esas en las que se supo qué hablante era el agente: la base de los porcentajes.</param>
/// <param name="LlamadasEspana">Llamadas en las que el agente dijo alguna palabra de España del nivel.</param>
/// <param name="LlamadasColombia">Llamadas en las que dijo alguna de Colombia del nivel.</param>
public sealed record EspanolizacionGaia(int Llamadas, int ConTranscripcion, int Base, int LlamadasEspana, int LlamadasColombia)
{
    public double? Espana => Base == 0 ? null : (double)LlamadasEspana / Base;
    public double? Colombia => Base == 0 ? null : (double)LlamadasColombia / Base;
}

/// <summary>Un par de palabras con cuántas llamadas usan cada lado.</summary>
public sealed record FilaParGaia(ParGaia Par, int LlamadasEspana, int LlamadasColombia, int Base)
{
    public double? Espana => Base == 0 ? null : (double)LlamadasEspana / Base;
    public double? Colombia => Base == 0 ? null : (double)LlamadasColombia / Base;
}

/// <summary>Una palabra con las llamadas en las que la dijo el agente.</summary>
public sealed record FilaPalabraGaia(PalabraGaia Palabra, int Llamadas, int Base)
{
    public double? Fraccion => Base == 0 ? null : (double)Llamadas / Base;
}

/// <summary>Un agente (o día, o etapa) con su españolización.</summary>
public sealed record GrupoEspanolizacion(string Clave, string Texto, EspanolizacionGaia Espanolizacion, IndicadoresGaia Indicadores);

/// <summary>Los análisis de las fases 3 y 4: repartos, motivos, obstáculos, semanas y españolización.</summary>
public static partial class CalculadoraGaia
{
    /// <summary>Valores de DataOrb que no dicen nada y se quitan de los repartos.</summary>
    private static readonly HashSet<string> SinValor = new(StringComparer.OrdinalIgnoreCase)
    {
        "", "NA", "N/A", "String", "Not applicable", "No aplicable", "No aplica", "No disponible", "null",
    };

    public static bool TieneValor(string texto) => !SinValor.Contains(texto.Trim());

    /// <summary>
    /// Reparto de <paramref name="valor"/> en las llamadas, de mayor a menor; lo que pasa de
    /// <paramref name="maximo"/> se junta en «Otros». Sin valor (NA, «No aplica»…) no cuenta.
    /// </summary>
    /// <param name="conOtros">Falso en textos libres (temas): «Otros» se llevaría casi todo y no dice nada.</param>
    public static List<RepartoGaia> Reparto(IEnumerable<LlamadaGaia> ll, Func<LlamadaGaia, string> valor, int maximo = 10, bool conOtros = true)
    {
        var conteo = ll.Select(valor).Where(TieneValor).GroupBy(v => v.Trim())
            .Select(g => (Texto: g.Key, Cantidad: g.Count())).OrderByDescending(x => x.Cantidad).ThenBy(x => x.Texto).ToList();
        return Recortar(conteo, maximo, conOtros);
    }

    /// <summary>
    /// Los obstáculos de la resolución (vienen varios por llamada, separados por « | »): en cuántas llamadas
    /// sale cada uno. La fracción es sobre todas las llamadas del filtro.
    /// </summary>
    public static List<RepartoGaia> Obstaculos(IReadOnlyCollection<LlamadaGaia> ll, int maximo = 15)
    {
        var conteo = ll.SelectMany(l => l.Obstaculos.Split('|', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries).Distinct())
            .Where(TieneValor).GroupBy(o => o.TrimEnd('.'))
            .Select(g => (Texto: g.Key, Cantidad: g.Count())).OrderByDescending(x => x.Cantidad).ThenBy(x => x.Texto)
            .Take(maximo).ToList();
        return conteo.Select(x => new RepartoGaia(x.Texto, x.Cantidad, ll.Count == 0 ? 0 : (double)x.Cantidad / ll.Count)).ToList();
    }

    private static List<RepartoGaia> Recortar(List<(string Texto, int Cantidad)> conteo, int maximo, bool conOtros = true)
    {
        var total = conteo.Sum(x => x.Cantidad);
        if (total == 0) return [];
        var lista = conteo.Take(maximo).Select(x => new RepartoGaia(x.Texto, x.Cantidad, (double)x.Cantidad / total)).ToList();
        var resto = conteo.Skip(maximo).Sum(x => x.Cantidad);
        if (resto > 0 && conOtros) lista.Add(new RepartoGaia("Otros", resto, (double)resto / total));
        return lista;
    }

    /// <summary>Por semana ISO (clave «2026-W37»; texto «Sem 37 · 07/09», con el lunes).</summary>
    public static List<GrupoGaia> PorSemana(IEnumerable<LlamadaGaia> ll)
        => ll.GroupBy(l => Lunes(l.Fecha))
             .OrderBy(g => g.Key)
             .Select(g => new GrupoGaia(
                 $"{ISOWeek.GetYear(g.Key.ToDateTime(TimeOnly.MinValue))}-W{ISOWeek.GetWeekOfYear(g.Key.ToDateTime(TimeOnly.MinValue)):00}",
                 $"Sem {ISOWeek.GetWeekOfYear(g.Key.ToDateTime(TimeOnly.MinValue))} · {g.Key:dd/MM}",
                 Calcular(g.ToList())))
             .ToList();

    /// <summary>Españolización por semana ISO (clave «2026-W37»; texto «Sem 37 · 07/09»).</summary>
    public static List<GrupoEspanolizacion> EspanolizacionPorSemana(IEnumerable<LlamadaGaia> ll, PalabrasGaia palabras, int nivel)
        => ll.GroupBy(l => Lunes(l.Fecha))
             .OrderBy(g => g.Key)
             .Select(g =>
             {
                 var lista = g.ToList();
                 var dia = g.Key.ToDateTime(TimeOnly.MinValue);
                 return new GrupoEspanolizacion(
                     $"{ISOWeek.GetYear(dia)}-W{ISOWeek.GetWeekOfYear(dia):00}",
                     $"Sem {ISOWeek.GetWeekOfYear(dia)} · {g.Key:dd/MM}",
                     Espanolizacion(lista, palabras, nivel), Calcular(lista));
             })
             .ToList();

    private static DateOnly Lunes(DateOnly d) => d.AddDays(-(((int)d.DayOfWeek + 6) % 7));

    /// <summary>El valor de un motivo en un nivel (1, 2 o 3).</summary>
    public static string Motivo(LlamadaGaia l, int nivel) => nivel switch { 1 => l.Motivo1, 2 => l.Motivo2, _ => l.Motivo3 };

    /// <summary>
    /// El árbol de motivos: cada nivel ordenado por volumen, con como mucho <paramref name="maximoHijos"/>
    /// ramas por nodo (el resto en «Otros»). Las llamadas sin motivo van en «Sin motivo».
    /// </summary>
    public static List<NodoMotivo> ArbolMotivos(IReadOnlyCollection<LlamadaGaia> ll, int maximoHijos = 12)
        => Ramas(ll, 1, maximoHijos);

    private static List<NodoMotivo> Ramas(IReadOnlyCollection<LlamadaGaia> ll, int nivel, int maximo)
    {
        if (nivel > 3 || ll.Count == 0) return [];
        var grupos = ll.GroupBy(l => TieneValor(Motivo(l, nivel)) ? Motivo(l, nivel) : "Sin motivo")
            .Select(g => (Clave: g.Key, Llamadas: g.ToList()))
            .OrderBy(g => g.Clave == "Sin motivo").ThenByDescending(g => g.Llamadas.Count).ToList();
        if (grupos.Count > maximo)
        {
            var otros = grupos.Skip(maximo).SelectMany(g => g.Llamadas).ToList();
            grupos = grupos.Take(maximo).Append(("Otros", otros)).ToList();
        }
        return grupos.Select(g => new NodoMotivo(
                g.Clave, nivel, Calcular(g.Llamadas), (double)g.Llamadas.Count / ll.Count,
                g.Clave is "Sin motivo" or "Otros" ? [] : Ramas(g.Llamadas, nivel + 1, maximo)))
            .ToList();
    }

    /// <summary>Indicadores por motivo de un nivel, de más a menos llamadas (página «Mapa de afectación»).</summary>
    public static List<GrupoGaia> PorMotivo(IEnumerable<LlamadaGaia> ll, int nivel, int maximo = 20)
        => ll.Where(l => TieneValor(Motivo(l, nivel)))
             .GroupBy(l => Motivo(l, nivel))
             .Select(g => new GrupoGaia(g.Key, g.Key, Calcular(g.ToList())))
             .OrderByDescending(g => g.Indicadores.Llamadas).ThenBy(g => g.Clave)
             .Take(maximo).ToList();

    // ------------------------------------------------------------------
    // Españolización
    // ------------------------------------------------------------------

    /// <summary>Los índices de palabras de España y de Colombia de los pares de un nivel.</summary>
    private static (HashSet<int> Espana, HashSet<int> Colombia) IndicesNivel(PalabrasGaia palabras, int nivel)
    {
        var pares = palabras.ParesDelNivel(nivel).ToList();
        return (pares.SelectMany(p => p.Espana).Select(p => p.Indice).ToHashSet(),
                pares.SelectMany(p => p.Colombia).Select(p => p.Indice).ToHashSet());
    }

    /// <summary>
    /// Españolización de un grupo de llamadas: sobre las llamadas con transcripción y agente identificado,
    /// cuántas tienen alguna palabra de España del nivel y cuántas alguna de Colombia.
    /// </summary>
    public static EspanolizacionGaia Espanolizacion(IReadOnlyCollection<LlamadaGaia> ll, PalabrasGaia palabras, int nivel)
    {
        var (es, col) = IndicesNivel(palabras, nivel);
        int conTranscripcion = 0, @base = 0, nEs = 0, nCol = 0;
        foreach (var l in ll)
        {
            if (l.TieneTranscripcion) conTranscripcion++;
            if (l.Palabras is not { } p) continue;
            @base++;
            if (p.Any(es.Contains)) nEs++;
            if (p.Any(col.Contains)) nCol++;
        }
        return new EspanolizacionGaia(ll.Count, conTranscripcion, @base, nEs, nCol);
    }

    /// <summary>Cada par del nivel con las llamadas que usan su palabra de España y su palabra de Colombia.</summary>
    public static List<FilaParGaia> ParesEspanolizacion(IReadOnlyCollection<LlamadaGaia> ll, PalabrasGaia palabras, int nivel)
    {
        var conDato = ll.Where(l => l.Palabras is not null).ToList();
        return palabras.ParesDelNivel(nivel).Select(par =>
        {
            var es = par.Espana.Select(p => p.Indice).ToHashSet();
            var col = par.Colombia.Select(p => p.Indice).ToHashSet();
            return new FilaParGaia(par, conDato.Count(l => l.Palabras!.Any(es.Contains)), conDato.Count(l => l.Palabras!.Any(col.Contains)), conDato.Count);
        }).ToList();
    }

    /// <summary>Las palabras del nivel con cuántas llamadas las dicen, de más a menos (la «nube de palabras» del PBI).</summary>
    public static List<FilaPalabraGaia> PalabrasEspanolizacion(IReadOnlyCollection<LlamadaGaia> ll, PalabrasGaia palabras, int nivel)
    {
        var conDato = ll.Where(l => l.Palabras is not null).ToList();
        var conteo = new Dictionary<int, int>();
        foreach (var i in conDato.SelectMany(l => l.Palabras!)) conteo[i] = conteo.GetValueOrDefault(i) + 1;
        return palabras.ParesDelNivel(nivel).SelectMany(p => p.Espana.Concat(p.Colombia))
            .Select(p => new FilaPalabraGaia(p, conteo.GetValueOrDefault(p.Indice), conDato.Count))
            .OrderByDescending(f => f.Llamadas).ThenBy(f => f.Palabra.Texto).ToList();
    }

    /// <summary>Españolización e indicadores agrupados por <paramref name="clave"/>.</summary>
    public static List<GrupoEspanolizacion> EspanolizacionPor(IEnumerable<LlamadaGaia> ll, PalabrasGaia palabras, int nivel,
        Func<LlamadaGaia, string> clave, Func<IGrouping<string, LlamadaGaia>, string> texto)
        => ll.GroupBy(clave)
             .Select(g =>
             {
                 var lista = g.ToList();
                 return new GrupoEspanolizacion(g.Key, texto(g), Espanolizacion(lista, palabras, nivel), Calcular(lista));
             })
             .ToList();
}
