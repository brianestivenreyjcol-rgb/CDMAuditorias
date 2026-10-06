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
            .Split(motivo.Trim(), "(?<=[a-zñáéíóú])(?=[A-ZÑÁÉÍÓÚ])")
            .Select(p => p == "Or" ? "o" : p.ToLowerInvariant());
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

    /// <summary>Lo que DataOrb escribe cuando no hay obstáculo («No aplicable», «Not applicable», «String»…).</summary>
    public static bool HayTexto(string valor)
        => !string.IsNullOrWhiteSpace(valor) && !new[] { "NA", "String", "Not applicable", "No aplicable", "No aplica" }.Contains(valor.Trim());

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
}
