using System.Diagnostics;
using CDM_Auditorias_Calidad.Models;
using CDM_Auditorias_Calidad.Servicios.Datos;
using Microsoft.AspNetCore.Mvc;

namespace CDM_Auditorias_Calidad.Controllers;

public sealed class HomeController : Controller
{
    private readonly AlmacenAuditorias _almacen;

    public HomeController(AlmacenAuditorias almacen) => _almacen = almacen;

    /// <summary>La portada («Menú» del Power BI).</summary>
    [HttpGet("/")]
    public IActionResult Index() => View(new MenuModelo(_almacen.Actual, _almacen.UltimoError));

    [Route("/error")]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
        => View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
}
