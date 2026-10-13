using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using CDM_Auditorias_Calidad.Servicios.Configuracion;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Options;

namespace CDM_Auditorias_Calidad.Servicios.Acceso;

/// <summary>
/// La contraseña del grupo «Personal» de la portada (Rellamada y No solución por sector, y Pesos y metas). Lo pidió el
/// usuario el 13-10-2026: la portada se divide en «General», abierto, y «Personal», con contraseña.
/// </summary>
/// <remarks>
/// La contraseña es <c>CLAVE_PERSONAL</c> del mismo .env que las credenciales de SQL (una variable de entorno con ese nombre
/// manda): así no queda en el repositorio, que es público. Si falta, «Personal» queda cerrado para todos. Al acertarla se da
/// un pase (cookie cifrada con Data Protection) que vale <see cref="Duracion"/>; lleva un resumen de la contraseña, así que
/// cambiarla en el .env cierra los pases que ya se dieron. Tras <see cref="FallosMaximos"/> fallos seguidos desde una misma IP,
/// esa IP espera <see cref="Espera"/>.
/// </remarks>
public sealed class AccesoPersonal
{
    public const string Cookie = "cdm_personal";
    public const string Variable = "CLAVE_PERSONAL";
    public const int FallosMaximos = 5;
    public static readonly TimeSpan Duracion = TimeSpan.FromHours(12);
    public static readonly TimeSpan Espera = TimeSpan.FromMinutes(5);

    private readonly ITimeLimitedDataProtector _protector;
    private readonly Func<string?> _leerClave;
    private readonly Func<DateTimeOffset> _ahora;
    private readonly ConcurrentDictionary<string, (int Fallos, DateTimeOffset Hasta)> _intentos = new();

    public AccesoPersonal(IDataProtectionProvider proteccion, IOptions<OpcionesAuditorias> opciones, IWebHostEnvironment entorno)
        : this(proteccion, () => LeerClave(Path.Combine(entorno.ContentRootPath, opciones.Value.RutaEnv)), () => DateTimeOffset.UtcNow)
    {
    }

    /// <summary>Para las pruebas: la contraseña y la hora llegan de fuera.</summary>
    public AccesoPersonal(IDataProtectionProvider proteccion, Func<string?> leerClave, Func<DateTimeOffset> ahora)
    {
        _protector = proteccion.CreateProtector("CDM.AccesoPersonal").ToTimeLimitedDataProtector();
        _leerClave = leerClave;
        _ahora = ahora;
    }

    /// <summary>Si hay contraseña configurada (si no, nadie puede entrar).</summary>
    public bool Configurada => !string.IsNullOrEmpty(_leerClave());

    /// <summary>Si esta IP tiene que esperar por demasiados fallos seguidos.</summary>
    public bool Bloqueada(string ip)
        => _intentos.TryGetValue(ip, out var i) && i.Fallos >= FallosMaximos && i.Hasta > _ahora();

    /// <summary>
    /// Comprueba la contraseña escrita. Si acierta devuelve el pase para la cookie; si no, null (y cuenta el fallo de la IP).
    /// </summary>
    public string? Entrar(string? intento, string ip)
    {
        var clave = _leerClave();
        if (string.IsNullOrEmpty(clave) || Bloqueada(ip)) return null;

        if (!Iguales(intento ?? "", clave))
        {
            _intentos.AddOrUpdate(ip,
                _ => (1, _ahora() + Espera),
                (_, i) => (i.Hasta <= _ahora() ? 1 : i.Fallos + 1, _ahora() + Espera));
            return null;
        }

        _intentos.TryRemove(ip, out _);
        return _protector.Protect(Resumen(clave), _ahora() + Duracion);
    }

    /// <summary>Si la cookie es un pase vigente y de la contraseña de ahora.</summary>
    public bool PaseValido(string? pase)
    {
        var clave = _leerClave();
        if (string.IsNullOrEmpty(pase) || string.IsNullOrEmpty(clave)) return false;
        try
        {
            var contenido = _protector.Unprotect(pase, out var caduca);
            return caduca > _ahora() && Iguales(contenido, Resumen(clave));
        }
        catch (CryptographicException)
        {
            return false; // Manipulada, caducada o de otra máquina.
        }
    }

    public bool PaseValido(HttpContext http) => PaseValido(http.Request.Cookies[Cookie]);

    public static string Ip(HttpContext http) => http.Connection.RemoteIpAddress?.ToString() ?? "?";

    private static string Resumen(string clave)
        => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes("CDM.Personal|" + clave)));

    private static bool Iguales(string a, string b)
        => CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(a), Encoding.UTF8.GetBytes(b));

    private static string? LeerClave(string rutaEnv)
    {
        var v = Environment.GetEnvironmentVariable(Variable);
        if (!string.IsNullOrWhiteSpace(v)) return v.Trim();
        return LectorDotEnv.Cargar(rutaEnv).TryGetValue(Variable, out var x) && !string.IsNullOrWhiteSpace(x) ? x.Trim() : null;
    }
}
