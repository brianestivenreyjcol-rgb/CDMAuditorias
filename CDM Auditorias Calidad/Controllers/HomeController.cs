using System.Diagnostics;
using System.Globalization;
using CDM_Auditorias_Calidad.Models;
using CDM_Auditorias_Calidad.Servicios.Datos;
using CDM_Auditorias_Calidad.Servicios.Gaia;
using CDM_Auditorias_Calidad.Servicios.NoSolucion;
using CDM_Auditorias_Calidad.Servicios.Pesos;
using CDM_Auditorias_Calidad.Servicios.Sectores;
using Microsoft.AspNetCore.Mvc;

namespace CDM_Auditorias_Calidad.Controllers;

public sealed class HomeController : Controller
{
    private readonly AlmacenAuditorias _almacen;
    private readonly ServicioNoSolucion _noSolucion;
    private readonly ServicioGaia _gaia;
    private readonly ServicioSectores _sectores;
    private readonly ServicioPesos _pesos;

    public HomeController(AlmacenAuditorias almacen, ServicioNoSolucion noSolucion, ServicioGaia gaia, ServicioSectores sectores, ServicioPesos pesos)
    {
        _almacen = almacen;
        _noSolucion = noSolucion;
        _gaia = gaia;
        _sectores = sectores;
        _pesos = pesos;
    }

    /// <summary>La portada: las cinco tarjetas de informe con el estado de sus datos.</summary>
    [HttpGet("/")]
    public IActionResult Index()
    {
        DateTime? nsCargado = _noSolucion.Cache.TieneCubo()
            && DateTime.TryParse(_noSolucion.Cache.Actual().Cubo.Meta.Generado, CultureInfo.InvariantCulture, DateTimeStyles.None, out var g)
            ? g : null;
        DateTime? gaiaCargado = _gaia.Actual is { } d && d.Generado != default ? d.Generado : null;
        _sectores.Revisar();
        DateTime? sectoresCargado = _sectores.Actual is { } s && s.Generado != default ? s.Generado : null;
        return View(new MenuModelo(_almacen.Actual, _almacen.UltimoError,
            new EstadoInforme(nsCargado, _noSolucion.ConstruyendoAhora),
            new EstadoInforme(gaiaCargado, _gaia.Cargando),
            new EstadoInforme(sectoresCargado, _sectores.Cargando),
            new EstadoInforme(_pesos.UltimoAnalisis()?.Analizado, _pesos.Analizando)));
    }

    [Route("/error")]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
        => View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
}
