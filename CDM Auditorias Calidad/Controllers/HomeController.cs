using System.Diagnostics;
using System.Globalization;
using CDM_Auditorias_Calidad.Models;
using CDM_Auditorias_Calidad.Servicios.Acceso;
using CDM_Auditorias_Calidad.Servicios.Datos;
using CDM_Auditorias_Calidad.Servicios.Gaia;
using CDM_Auditorias_Calidad.Servicios.NoSolucion;
using CDM_Auditorias_Calidad.Servicios.Pesos;
using CDM_Auditorias_Calidad.Servicios.Sectores;
using Microsoft.AspNetCore.Mvc;

namespace CDM_Auditorias_Calidad.Controllers;

/// <summary>
/// La portada y sus dos grupos (13-10-2026, pedido del usuario): «General» (<c>/informes</c>: Auditorías, CDM No solución
/// y GAIA Formación), abierto, y «Personal» (<c>/personal</c>: por sector y Pesos y metas), con contraseña.
/// </summary>
public sealed class HomeController : Controller
{
    public const string RutaGeneral = "/informes";
    public const string RutaPersonal = "/personal";

    private readonly AlmacenAuditorias _almacen;
    private readonly ServicioNoSolucion _noSolucion;
    private readonly ServicioGaia _gaia;
    private readonly ServicioSectores _sectores;
    private readonly ServicioPesos _pesos;
    private readonly AccesoPersonal _acceso;

    public HomeController(AlmacenAuditorias almacen, ServicioNoSolucion noSolucion, ServicioGaia gaia, ServicioSectores sectores, ServicioPesos pesos,
        AccesoPersonal acceso)
    {
        _almacen = almacen;
        _noSolucion = noSolucion;
        _gaia = gaia;
        _sectores = sectores;
        _pesos = pesos;
        _acceso = acceso;
    }

    /// <summary>La portada: las dos tarjetas de grupo.</summary>
    [HttpGet("/")]
    public IActionResult Index()
    {
        var conDatos = (_almacen.Actual is not null ? 1 : 0) + (EstadoNoSolucion().Cargado is not null ? 1 : 0) + (EstadoGaia().Cargado is not null ? 1 : 0);
        return View(new PortadaModelo(conDatos, _acceso.PaseValido(HttpContext), _acceso.Configurada));
    }

    /// <summary>Grupo «General»: las tarjetas de Auditorías, CDM No solución y GAIA Formación con el estado de sus datos.</summary>
    [HttpGet(RutaGeneral)]
    public IActionResult General()
        => View(new MenuModelo(_almacen.Actual, _almacen.UltimoError, EstadoNoSolucion(), EstadoGaia()));

    /// <summary>Grupo «Personal»: las tarjetas de por sector y de Pesos y metas.</summary>
    [HttpGet(RutaPersonal)]
    [AccesoPersonal]
    public IActionResult Personal()
    {
        _sectores.Revisar();
        DateTime? sectoresCargado = _sectores.Actual is { } s && s.Generado != default ? s.Generado : null;
        return View(new PersonalModelo(
            new EstadoInforme(sectoresCargado, _sectores.Cargando),
            new EstadoInforme(_pesos.UltimoAnalisis()?.Analizado, _pesos.Analizando)));
    }

    [HttpGet(RutaPersonal + "/entrar")]
    public IActionResult Entrar(string? volver)
    {
        if (_acceso.PaseValido(HttpContext)) return LocalRedirect(Destino(volver));
        return View(new EntrarModelo(Destino(volver), _acceso.Configurada ? null : "Aún no hay contraseña configurada en el servidor."));
    }

    [HttpPost(RutaPersonal + "/entrar")]
    [ValidateAntiForgeryToken]
    public IActionResult Entrar(string? clave, string? volver)
    {
        var ip = AccesoPersonal.Ip(HttpContext);
        if (_acceso.Entrar(clave, ip) is { } pase)
        {
            Response.Cookies.Append(AccesoPersonal.Cookie, pase, new CookieOptions
            {
                HttpOnly = true,
                SameSite = SameSiteMode.Lax,
                Secure = Request.IsHttps,
                IsEssential = true,
                MaxAge = AccesoPersonal.Duracion,
            });
            return LocalRedirect(Destino(volver));
        }

        var error = !_acceso.Configurada ? "Aún no hay contraseña configurada en el servidor."
            : _acceso.Bloqueada(ip) ? "Demasiados intentos seguidos. Espera unos minutos y vuelve a probar."
            : "La contraseña no es correcta.";
        Response.StatusCode = StatusCodes.Status401Unauthorized;
        return View(new EntrarModelo(Destino(volver), error));
    }

    /// <summary>«Cerrar Personal»: borra el pase de este navegador.</summary>
    [HttpPost(RutaPersonal + "/salir")]
    [ValidateAntiForgeryToken]
    public IActionResult Salir()
    {
        Response.Cookies.Delete(AccesoPersonal.Cookie);
        return LocalRedirect("/");
    }

    [Route("/error")]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
        => View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });

    private EstadoInforme EstadoNoSolucion()
    {
        DateTime? cargado = _noSolucion.Cache.TieneCubo()
            && DateTime.TryParse(_noSolucion.Cache.Actual().Cubo.Meta.Generado, CultureInfo.InvariantCulture, DateTimeStyles.None, out var g)
            ? g : null;
        return new EstadoInforme(cargado, _noSolucion.ConstruyendoAhora);
    }

    private EstadoInforme EstadoGaia()
        => new(_gaia.Actual is { } d && d.Generado != default ? d.Generado : null, _gaia.Cargando);

    /// <summary>Solo se vuelve a una dirección de esta web (nunca a otra, aunque venga en la URL).</summary>
    private string Destino(string? volver)
        => !string.IsNullOrEmpty(volver) && Url.IsLocalUrl(volver) && !volver.StartsWith(RutaPersonal + "/entrar", StringComparison.OrdinalIgnoreCase)
            ? volver : RutaPersonal;
}
