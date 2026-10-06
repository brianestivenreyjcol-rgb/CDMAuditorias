using System.Text.RegularExpressions;
using CDM_Auditorias_Calidad.Infraestructura;
using CDM_Auditorias_Calidad.Servicios.Gaia;
using CDM_Auditorias_Calidad.Models.Gaia;

namespace CDM_Auditorias_Calidad.Views.Gaia;

/// <summary>
/// Pequeños ayudantes de presentación de las vistas de GAIA Formación: solo dan forma a lo que se pinta
/// (etiquetas, formatos y la clase de color de cada umbral). Los cálculos viven en <see cref="CalculadoraGaia"/>.
/// Va en la carpeta de las vistas porque <c>@functions</c> no funciona en <c>_ViewImports</c>; se importa allí
/// con <c>@using static</c>.
/// </summary>
public static class AyudasGaia
{
    /// <summary>Una llamada con menos de esto no cuenta como máximo o mínimo de un día, ni se colorea un agente.</summary>
    public const int MinLlamadasFiable = 10;

    /// <summary>«1 Preconexion» → «Pre 1»; «Aseguramiento 3» → «Aseg. 3» (para los ejes).</summary>
    public static string EtapaCorta(string etapa)
    {
        if (etapa.EndsWith("Preconexion", StringComparison.OrdinalIgnoreCase)) return "Pre " + etapa.Split(' ')[0];
        if (etapa.StartsWith("Aseguramiento", StringComparison.OrdinalIgnoreCase)) return "Aseg. " + etapa.Split(' ').Last();
        return etapa;
    }

    /// <summary>Etapa con tilde para leerla: «1 Preconexion» → «1 Preconexión».</summary>
    public static string EtapaLegible(string etapa) => etapa.Replace("Preconexion", "Preconexión");

    /// <summary>«incidenciaOrReclamación» → «Incidencia o reclamación»; «NA» o vacío → «—».</summary>
    public static string MotivoLegible(string motivo)
    {
        if (string.IsNullOrWhiteSpace(motivo) || motivo == "NA") return "—";
        var partes = Regex
            .Split(Regex.Replace(motivo.Trim(), "(?<=[a-zñáéíóú])O(?=[A-ZÑÁÉÍÓÚ])", "Or"), "(?<=[a-zñáéíóú])(?=[A-ZÑÁÉÍÓÚ])")
            .Select(p => p == "Or" ? "o" : ConTildes(p.ToLowerInvariant()));
        var texto = string.Join(" ", partes);
        return char.ToUpper(texto[0], Formato.Es) + texto[1..];
    }

    /// <summary>«yes» → Sí, «no» → No, vacío → «—» y cualquier otra cosa (NA, notApplicable) → N/A.</summary>
    public static string Calificacion(string valor)
        => valor.Length == 0 ? "—" : valor.Equals("yes", StringComparison.OrdinalIgnoreCase) ? "Sí"
           : valor.Equals("no", StringComparison.OrdinalIgnoreCase) ? "No" : "N/A";

    public static string ChipCalificacion(string valor)
        => valor.Equals("yes", StringComparison.OrdinalIgnoreCase) ? "chip chip-bueno"
           : valor.Equals("no", StringComparison.OrdinalIgnoreCase) ? "chip chip-critico" : "chip";

    public static string SiNo(bool? valor) => valor switch { true => "Sí", false => "No", _ => "—" };

    public static string RellamadaTexto(int? valor) => valor switch { 1 => "Sí", 0 => "No", _ => "—" };

    public static string EncuestaTexto(int? valor) => valor switch { 1 => "Resuelto", 2 => "No resuelto", _ => "—" };

    /// <summary>Segundos → m:ss («5:32»); vacío → «—».</summary>
    public static string Duracion(double? segundos)
    {
        if (segundos is not { } s) return "—";
        var total = (int)Math.Round(s);
        return $"{total / 60}:{total % 60:00}";
    }

    public static string Texto(string valor) => string.IsNullOrWhiteSpace(valor) || valor == "NA" ? "—" : valor;

    /// <summary>Lo que DataOrb escribe cuando no hay dato («No aplicable», «Not applicable», «String», «NA»…).</summary>
    public static bool HayTexto(string valor) => CalculadoraGaia.TieneValor(valor);

    /// <summary>Clase de tono de la barra de adherencia según los umbrales del PBI (≤ 40 %, ≤ 70 %, más).</summary>
    public static string TonoAdherencia(double? v)
        => v is not { } a ? "tono-neutro"
           : a <= PaginaEstiloGaia.UmbralBajo ? "tono-critico" : a <= PaginaEstiloGaia.UmbralMedio ? "tono-atencion" : "tono-bueno";

    /// <summary>Clase del semáforo pastel de una celda de adherencia.</summary>
    public static string MapaAdherencia(double? v)
        => v is not { } a ? ""
           : a <= PaginaEstiloGaia.UmbralBajo ? "mapa-rojo" : a <= PaginaEstiloGaia.UmbralMedio ? "mapa-ambar" : "mapa-verde";

    /// <summary>
    /// Si un valor es mejor o peor que el total del filtro (más alto o más bajo es mejor según el indicador).
    /// Margen: un punto porcentual o el 10 % del total, lo que sea mayor. Con pocas llamadas no se compara.
    /// </summary>
    public static string ClaseFrenteAlTotal(KpiGaia kpi, IndicadoresGaia fila, IndicadoresGaia total)
    {
        if (fila.Llamadas < MinLlamadasFiable) return "";
        if (kpi.Valor(fila) is not { } v || kpi.Valor(total) is not { } t) return "";
        var margen = Math.Max(0.01, t * 0.10);
        if (Math.Abs(v - t) <= margen) return "";
        return (v > t) == kpi.MejorAlto ? "mejor-total" : "peor-total";
    }

    /// <summary>Para la ficha de las gráficas: «Rellamada 72 h» → «Rellamada (72 h)» (la ficha parte donde empieza la primera cifra).</summary>
    public static string NombreFicha(string nombre) => nombre.Replace(" 72 h", " (72 h)").Replace(" 24 h", " (24 h)");

    /// <summary>Color fijo de cada marca en las piezas (el mismo que en No solución).</summary>
    public static string ClaseMarca(string marca) => marca switch
    {
        "YOIGO" => "serie-yoigo", "MASMOVIL" => "serie-masmovil", "JAZZTEL" => "serie-jazztel", "ORANGE" => "serie-orange", _ => "serie-otra",
    };

    public static string DiaLargo(string clave) => DateOnly.TryParse(clave, out var d) ? Formato.Fecha(d) : clave;

    public static string P(double fraccion) => Formato.Coord(fraccion * 100) + "%";
    public static string C(double fraccion) => Formato.Coord(fraccion * 1000);

    // ---------------------------------------------------------------------------------------
    // Textos largos, motivos y obstáculos
    // ---------------------------------------------------------------------------------------

    private static readonly Dictionary<string, string> Tildes = new(StringComparer.OrdinalIgnoreCase)
    {
        ["movil"] = "móvil", ["tecnico"] = "técnico", ["logistica"] = "logística", ["telefonia"] = "telefonía",
        ["generico"] = "genérico", ["erronea"] = "errónea", ["erroneo"] = "erróneo", ["area"] = "área", ["mas"] = "más",
    };

    /// <summary>Un motivo de DataOrb a veces llega sin tildes («facturacionOrCobros»): se las pone a lo habitual.</summary>
    private static string ConTildes(string palabra)
    {
        if (Tildes.TryGetValue(palabra, out var t)) return t;
        return palabra.Length > 4 && palabra.EndsWith("ion", StringComparison.Ordinal) ? palabra[..^3] + "ión" : palabra;
    }

    /// <summary>Los obstáculos de una llamada: vienen separados por « | »; sin los que no dicen nada (NA, String…).</summary>
    public static IReadOnlyList<string> ObstaculosDe(string texto)
        => texto.Split('|', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
                .Where(CalculadoraGaia.TieneValor).Distinct().ToList();

    /// <summary>
    /// Un resumen largo («Objetivo del Agente: …\nCatalizador del Contacto: …») en párrafos, cada uno con su rótulo
    /// (lo que va antes de los primeros dos puntos, si es corto) aparte del texto.
    /// </summary>
    public static IReadOnlyList<(string? Rotulo, string Texto)> Parrafos(string resumen)
        => resumen.Split('\n', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries).Select(linea =>
        {
            var i = linea.IndexOf(": ", StringComparison.Ordinal);
            return i is > 2 and < 45 && !linea[..i].Contains('.') ? ((string?)linea[..i], linea[(i + 2)..]) : (null, linea);
        }).ToList();

    // ---------------------------------------------------------------------------------------
    // Gráficas genéricas (varias series, día / semana / etapa, porcentajes o segundos)
    // ---------------------------------------------------------------------------------------

    /// <summary>Una línea o un grupo de columnas: sus valores en el orden de los puntos del gráfico.</summary>
    /// <param name="Clase">Clase de color fijo (<c>serie-a</c> … <c>serie-e</c>, <c>serie-espana</c>, <c>serie-colombia</c>).</param>
    public sealed record SerieGaia(string Nombre, string Clase, IReadOnlyList<double?> Valores);

    /// <summary>
    /// Lo que necesitan las gráficas de línea y de columnas: un punto por día, semana o etapa.
    /// <paramref name="Volumen"/> es lo que se enseña en la ficha (llamadas o base) y lo que decide qué puntos pueden ser máximo o mínimo.
    /// </summary>
    public sealed record GraficoGaia(IReadOnlyList<string> Etiquetas, IReadOnlyList<string> Titulos, IReadOnlyList<int> Volumen, IReadOnlyList<SerieGaia> Series)
    {
        /// <summary>Los valores son segundos (TMO) y no fracciones.</summary>
        public bool Segundos { get; init; }
        public string NombreVolumen { get; init; } = "Llamadas";
        /// <summary>Línea discontinua de la media (solo con una serie).</summary>
        public double? Media { get; init; }
        public IReadOnlyList<double> Referencias { get; init; } = [];
        /// <summary>Etiquetas del eje X (las líneas con el volumen debajo van sin ellas).</summary>
        public bool EjeX { get; init; } = true;
        public int MaxEtiquetasX { get; init; } = 12;
        public string Descripcion { get; init; } = "";
    }

    /// <summary>Un valor de un gráfico escrito como se lee: m:ss o porcentaje.</summary>
    public static string Valor(double? valor, bool segundos) => segundos ? Duracion(valor) : Formato.Porcentaje(valor);

    /// <summary>Segundos → minutos para el eje («2,5 min»).</summary>
    public static string Minutos(double segundos) => (segundos / 60).ToString("0.#", Formato.Es) + " min";

    private static GraficoGaia Construir(string escala, IReadOnlyList<(string Clave, string Texto, int Volumen)> puntos,
        IEnumerable<(string Nombre, string Clase, IReadOnlyList<double?> Valores)> series)
        => new(
            puntos.Select(p => escala switch { "dia" => DateOnly.TryParse(p.Clave, out var d) ? Formato.Fecha(d) : p.Texto, "etapa" => EtapaCorta(p.Texto), _ => p.Texto }).ToList(),
            puntos.Select(p => escala switch { "dia" => DiaLargo(p.Clave), "etapa" => EtapaLegible(p.Texto), _ => p.Texto.Replace(" · ", " del ") }).ToList(),
            puntos.Select(p => p.Volumen).ToList(),
            series.Select(s => new SerieGaia(s.Nombre, s.Clase, s.Valores)).ToList())
        {
            MaxEtiquetasX = escala == "semana" ? 6 : 12,
        };

    /// <summary>Gráfico de grupos (días, semanas o etapas) con una serie por indicador.</summary>
    public static GraficoGaia Grafico(string escala, IReadOnlyList<GrupoGaia> grupos, params (string Nombre, string Clase, Func<IndicadoresGaia, double?> Valor)[] series)
        => Construir(escala, grupos.Select(g => (g.Clave, g.Texto, g.Indicadores.Llamadas)).ToList(),
            series.Select(s => (s.Nombre, s.Clase, (IReadOnlyList<double?>)grupos.Select(g => s.Valor(g.Indicadores)).ToList())));

    /// <summary>Gráfico de españolización: una serie con España y otra con Colombia; el volumen es la base.</summary>
    public static GraficoGaia GraficoEspanolizacion(string escala, IReadOnlyList<GrupoEspanolizacion> grupos)
        => Construir(escala, grupos.Select(g => (g.Clave, g.Texto, g.Espanolizacion.Base)).ToList(),
            [
                ("España", "serie-espana", grupos.Select(g => g.Espanolizacion.Espana).ToList()),
                ("Colombia", "serie-colombia", grupos.Select(g => g.Espanolizacion.Colombia).ToList()),
            ]) with { NombreVolumen = "Base" };

    // ---------------------------------------------------------------------------------------
    // Selectores, dispersión y barras apiladas
    // ---------------------------------------------------------------------------------------

    public sealed record OpcionSelectorGaia(string Texto, string Url, bool Activa);

    /// <summary>Un desplegable de enlaces (el indicador de las gráficas, los ejes de la dispersión).</summary>
    public sealed record SelectorGaia(string Id, string Titulo, string Actual, IReadOnlyList<OpcionSelectorGaia> Opciones);

    public sealed record PuntoGaia(string Nombre, string Detalle, double X, double Y, int Llamadas);

    /// <summary>Los agentes enfrentados en dos indicadores; <paramref name="TotalX"/> y <paramref name="TotalY"/> son las líneas de referencia.</summary>
    public sealed record DispersionGaia(IReadOnlyList<PuntoGaia> Puntos, KpiGaia X, KpiGaia Y, double? TotalX, double? TotalY);

    /// <summary>Un tramo de una barra al 100 % apilada.</summary>
    public sealed record TramoGaia(string Nombre, string Clase, double Fraccion);

    /// <summary>Clase de tono de un sentimiento («Positivo», «Neutro», «Mixto», «Negativo»).</summary>
    public static string ClaseSentimiento(string texto) => texto switch
    {
        "Positivo" => "tono-bueno", "Negativo" => "tono-critico", "Mixto" => "tono-atencion", _ => "tono-neutro",
    };
}
