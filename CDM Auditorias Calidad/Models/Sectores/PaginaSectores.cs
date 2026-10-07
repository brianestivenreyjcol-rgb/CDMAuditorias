using CDM_Auditorias_Calidad.Servicios.Sectores;
using Microsoft.AspNetCore.WebUtilities;

namespace CDM_Auditorias_Calidad.Models.Sectores;

/// <summary>
/// Una pestaña de «Rellamada y No solución por sector» (las dos tienen la misma forma). Sin datos todavía
/// (<see cref="Preparando"/>), la vista enseña «preparando» y no hay filtros.
/// </summary>
public sealed class PaginaSectores
{
    public const string RutaBase = "/sectores";
    public const string Titulo = "Rellamada y No solución por sector";

    public required IndicadorSectores Indicador { get; init; }
    public DatosSectores? Datos { get; init; }

    /// <summary>Nulo mientras no hay datos.</summary>
    public ResultadoSectores? R { get; init; }
    public bool Cargando { get; init; }
    public string? Error { get; init; }

    /// <summary>Resultado de pulsar «Actualizar ahora»: <c>en-curso</c> o <c>ya-en-curso</c>.</summary>
    public string? Actualizacion { get; init; }

    public bool Preparando => R is null;

    public IReadOnlyList<string> Avisos => Datos?.Avisos ?? [];

    public static string Ruta(IndicadorSectores i) => i == IndicadorSectores.NoSolucion ? RutaBase + "/nosolucion" : RutaBase;

    /// <summary>La otra pestaña (o esta) con los mismos filtros y el mismo mes.</summary>
    public string UrlIndicador(IndicadorSectores i) => Con(Ruta(i), R?.Parametros() ?? []);

    /// <summary>Esta pestaña con otro mes de referencia.</summary>
    public string UrlMes(string mes) => Con(Ruta(Indicador), (R?.Parametros(conMes: false) ?? []).Append(new("mes", mes)));

    public string UrlLimpiar => Ruta(Indicador);

    /// <summary>CSV con las filas de los dos indicadores y estos filtros (sin el mes: van todos).</summary>
    public string UrlCsv => Con(RutaBase + "/csv", R?.Parametros(conMes: false) ?? []);

    /// <summary>Marca de color de la página (guía 2.1): solo YGMM → morado, solo JAZZTEL → amarillo, solo ORANGE → naranja.</summary>
    public string MarcaPagina
    {
        get
        {
            var marcas = R?.Peticion.Marca ?? [];
            return marcas.Count == 1 ? marcas[0] switch { "YGMM" => "ygmm", "JAZZTEL" => "jazztel", "ORANGE" => "orange", _ => "orange" } : "orange";
        }
    }

    /// <summary>Clase de color fijo de una marca (cuando cada barra es de una marca, guía 2.2).</summary>
    public static string ClaseMarca(string marca) => marca switch
    {
        "YGMM" => "serie-yoigo",
        "JAZZTEL" => "serie-jazztel",
        "ORANGE" => "serie-orange",
        _ => "serie-otra",
    };

    /// <summary>
    /// Diferencia entre dos porcentajes (en fracción) escrita como resta, con signo y «%» (guía 9.5): «+0,58 %», «−1,20 %».
    /// </summary>
    public static string Diferencia(double fraccion)
    {
        var v = Math.Round(fraccion * 100, 2);
        var signo = v > 0 ? "+" : v < 0 ? "−" : "";
        return signo + Math.Abs(v).ToString("0.00", Infraestructura.Formato.Es) + Infraestructura.Formato.EspacioFino + "%";
    }

    /// <summary>Más es peor en los dos indicadores: sube más de un cuarto de punto → peor; baja → mejor; si no, igual.</summary>
    public static (string Clase, string Icono, string Chip) Tono(double? diferencia) => diferencia switch
    {
        > 0.0025 => ("peor", "sube", "chip-critico"),
        < -0.0025 => ("mejor", "baja", "chip-bueno"),
        _ => ("neutra", "igual", ""),
    };

    /// <summary>«15.311 de 72.771»: la ficha de una cifra.</summary>
    public static string DeCuantos(CifraSector c)
        => c.Casos.ToString("#,##0", Infraestructura.Formato.Es) + " de " + c.Base.ToString("#,##0", Infraestructura.Formato.Es);

    public static string Entero(long v) => v.ToString("#,##0", Infraestructura.Formato.Es);

    private static string Con(string ruta, IEnumerable<KeyValuePair<string, string>> parametros)
        => QueryHelpers.AddQueryString(ruta, parametros.Select(p => new KeyValuePair<string, string?>(p.Key, p.Value)));
}
