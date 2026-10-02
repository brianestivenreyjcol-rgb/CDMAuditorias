using CDM_Auditorias_Calidad.Servicios.Configuracion;
using CDM_Auditorias_Calidad.Servicios.Datos;
using CDM_Auditorias_Calidad.Servicios.Exportacion;
using CDM_Auditorias_Calidad.Servicios.Tablero;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace CDM_Auditorias_Calidad.Controllers;

/// <summary>
/// Las páginas «General» y «Formación &amp; Calidad» del Power BI.
/// </summary>
public sealed class TableroController : Controller
{
    private readonly AlmacenAuditorias _almacen;
    private readonly IOptions<OpcionesAuditorias> _opciones;

    public TableroController(AlmacenAuditorias almacen, IOptions<OpcionesAuditorias> opciones)
    {
        _almacen = almacen;
        _opciones = opciones;
    }

    /// <summary>
    /// La página con los filtros de la URL. Con la cabecera <c>X-Parcial: 1</c> (la manda
    /// site.js al cambiar un filtro) devuelve solo el tablero, sin el layout.
    /// </summary>
    [HttpGet("/{pagina:regex(^(general|formacion)$)}")]
    public async Task<IActionResult> Index(string pagina, [FromQuery] FiltrosTablero filtros, CancellationToken ct)
    {
        var datos = await _almacen.ObtenerAsync(ct);
        var modelo = CalculadoraTablero.Calcular(datos, Pagina(pagina), filtros, _opciones.Value.MetaCalidad, _almacen.UltimoError, _opciones.Value.CargosAgente);

        if (Request.Headers["X-Parcial"] == "1") return PartialView("_Informe", modelo);
        return View(modelo);
    }

    [HttpGet("/{pagina:regex(^(general|formacion)$)}/descargar")]
    public async Task<IActionResult> Descargar(string pagina, [FromQuery] FiltrosTablero filtros, CancellationToken ct)
    {
        var datos = await _almacen.ObtenerAsync(ct);
        if (datos is null) return Problem("Los datos de auditorías no están disponibles ahora mismo.", statusCode: 503);

        var p = Pagina(pagina);
        var filas = CalculadoraTablero.Detalle(datos, p, filtros);
        var titulo = $"Auditorías {p.Titulo}";
        var nombre = $"Auditorias_{p.Clave}_{DateTime.Now:yyyyMMdd_HHmm}.xlsx";
        return File(ExportadorExcel.Generar(filas, titulo), ExportadorExcel.TipoContenido, nombre);
    }

    /// <summary>Botón «Actualizar datos»: vuelve a ejecutar la consulta ahora.</summary>
    [HttpPost("/datos/recargar")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Recargar(string? volver, CancellationToken ct)
    {
        await _almacen.RecargarAsync(ct);
        return LocalRedirect(Url.IsLocalUrl(volver) ? volver! : "/");
    }

    private PaginaTablero Pagina(string clave) => clave == PaginaTablero.ClaveFormacion
        ? PaginaTablero.Formacion(_opciones.Value.CargosFormacion)
        : PaginaTablero.General;
}
