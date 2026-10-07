using System.Globalization;
using CDM_Auditorias_Calidad.Models.Sectores;
using CDM_Auditorias_Calidad.Servicios.Comun;
using CDM_Auditorias_Calidad.Servicios.Sectores;
using Microsoft.AspNetCore.Mvc;

namespace CDM_Auditorias_Calidad.Controllers;

/// <summary>
/// «Rellamada y No solución por sector»: la cifra mensual de cada sector de Bogotá. YGMM sale de BigQuery (la tabla
/// corporativa del PBI de Atención YGMM) y Jazztel, Orange y WhatsApp, de SQL Server (Indicadores).
/// </summary>
/// <remarks>
/// Como en No solución y GAIA, con la cabecera <c>X-Parcial: 1</c> (la manda site.js) cada pestaña devuelve solo
/// <c>#informe</c>.
/// </remarks>
[Route(PaginaSectores.RutaBase)]
public sealed class SectoresController : Controller
{
    private const string ClaveActualizacion = "SectoresActualizar";

    private readonly ServicioSectores _sectores;

    public SectoresController(ServicioSectores sectores) => _sectores = sectores;

    [HttpGet("")]
    public IActionResult Rellamada([FromQuery] PeticionSectores filtros) => Pagina(IndicadorSectores.Rellamada, filtros);

    [HttpGet("nosolucion")]
    public IActionResult NoSolucion([FromQuery] PeticionSectores filtros) => Pagina(IndicadorSectores.NoSolucion, filtros);

    /// <summary>Las filas de los dos indicadores con los filtros de marca y sector, en CSV para Excel.</summary>
    [HttpGet("csv")]
    public IActionResult Csv([FromQuery] PeticionSectores filtros)
    {
        _sectores.Revisar();
        if (_sectores.Actual is not { } datos) return NotFound();
        var filas = datos.Filas
            .Where(f => (filtros.Marca.Count == 0 || filtros.Marca.Contains(f.Marca)) && (filtros.Sector.Count == 0 || filtros.Sector.Contains(f.Sector)))
            .OrderBy(f => f.Indicador).ThenBy(f => CalculadoraSectores.OrdenMarca(f.Marca)).ThenBy(f => f.Sector, StringComparer.CurrentCulture).ThenBy(f => f.Mes, StringComparer.Ordinal);
        Response.Headers.CacheControl = "private, no-store";
        return File(ExportacionCsv.Generar(filas, ColumnasCsv), ExportacionCsv.TipoContenido,
            $"rellamada_nosolucion_por_sector_{DateTime.Now:yyyyMMdd_HHmm}.csv");
    }

    [HttpPost("actualizar")]
    [ValidateAntiForgeryToken]
    public IActionResult Actualizar(string? volver)
    {
        TempData[ClaveActualizacion] = _sectores.PedirActualizacion() ? "en-curso" : "ya-en-curso";
        var destino = !string.IsNullOrEmpty(volver) && Url.IsLocalUrl(volver)
                      && volver.StartsWith(PaginaSectores.RutaBase, StringComparison.OrdinalIgnoreCase)
            ? volver : PaginaSectores.RutaBase;
        return LocalRedirect(destino);
    }

    private IActionResult Pagina(IndicadorSectores indicador, PeticionSectores filtros)
    {
        _sectores.Revisar();
        var datos = _sectores.Actual;
        ViewData["Parcial"] = Request.Headers["X-Parcial"] == "1";
        return View("Indicador", new PaginaSectores
        {
            Indicador = indicador,
            Datos = datos,
            R = datos is null ? null : CalculadoraSectores.Resolver(datos, filtros, indicador, DateOnly.FromDateTime(DateTime.Today)),
            Cargando = _sectores.Cargando,
            Error = _sectores.UltimoError,
            Actualizacion = TempData[ClaveActualizacion] as string,
        });
    }

    private static string Pct(FilaSector f)
        => f.Base > 0 ? ((double)f.Casos / f.Base * 100).ToString("0.00", CultureInfo.GetCultureInfo("es-ES")) : "";

    private static readonly IReadOnlyList<ColumnaCsv<FilaSector>> ColumnasCsv =
    [
        new("Indicador", f => IndicadorSectores.De(f.Indicador).Titulo),
        new("Mes", f => f.Mes),
        new("Marca", f => CalculadoraSectores.TextoMarca(f.Marca)),
        new("Sector", f => f.Sector),
        new("Base", f => f.Base.ToString(CultureInfo.InvariantCulture)),
        new("Casos", f => f.Casos.ToString(CultureInfo.InvariantCulture)),
        new("Porcentaje", Pct),
        new("Fuente", f => f.Fuente),
    ];
}
