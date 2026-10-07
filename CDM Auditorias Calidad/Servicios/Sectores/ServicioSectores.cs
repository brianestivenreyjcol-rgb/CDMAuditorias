using System.Text.Json;
using CDM_Auditorias_Calidad.Servicios.Configuracion;
using Microsoft.Extensions.Options;

namespace CDM_Auditorias_Calidad.Servicios.Sectores;

/// <summary>
/// Los datos de «Rellamada y No solución por sector»: la caché en disco y la recarga en segundo plano. Ninguna
/// petición espera a las consultas: sin datos, la vista enseña «preparando».
/// </summary>
/// <remarks>
/// Se vuelve a traer todo (unos segundos) cuando los datos tienen más de <see cref="OpcionesSectores.RefrescoHoras"/>
/// horas y con «Actualizar ahora». Son pocos cientos de filas: la caché es pequeña.
/// </remarks>
public sealed class ServicioSectores
{
    /// <summary>Tiempo mínimo entre dos «Actualizar».</summary>
    public const int MinSegundosEntreRecargas = 30;

    /// <summary>Tras un fallo, no se reintenta solo hasta pasado este tiempo.</summary>
    public static readonly TimeSpan ReintentoTrasFallo = TimeSpan.FromMinutes(15);

    private readonly OpcionesSectores _op;
    private readonly string _rutaCache;
    private readonly FuenteSectores _fuente;
    private readonly ILogger<ServicioSectores> _log;
    private readonly object _cerrojo = new();
    private Task? _carga;
    private DateTime _ultimaPeticion = DateTime.MinValue;
    private DateTime _ultimoFallo = DateTime.MinValue;
    private bool _cacheLeida;

    public ServicioSectores(IOptions<OpcionesSectores> opciones, IOptions<OpcionesAuditorias> auditorias,
        IWebHostEnvironment entorno, ILogger<ServicioSectores> log)
    {
        _op = opciones.Value;
        _log = log;
        _rutaCache = Path.IsPathRooted(_op.RutaCache) ? _op.RutaCache : Path.GetFullPath(Path.Combine(entorno.ContentRootPath, _op.RutaCache));
        // El .env se lee en cada carga (el mismo de Auditorías): un cambio de contraseña no obliga a reiniciar.
        var rutaEnv = Path.Combine(entorno.ContentRootPath, auditorias.Value.RutaEnv);
        _fuente = new FuenteSectores(_op.Odbc, Path.Combine(entorno.ContentRootPath, "Consultas"),
            () => DatosConexion.Cargar(rutaEnv), _op.SegundosConsulta, log);
    }

    /// <summary>Los datos que se sirven ahora (nulo hasta la primera carga).</summary>
    public DatosSectores? Actual { get; private set; }

    /// <summary>El último error al traer los datos (se borra al cargar bien).</summary>
    public string? UltimoError { get; private set; }

    public double RefrescoHoras => _op.RefrescoHoras;

    public bool Cargando
    {
        get { lock (_cerrojo) { return _carga is { IsCompleted: false }; } }
    }

    /// <summary>Lo que se hace al entrar alguien y cada rato: lee la caché la primera vez y recarga si hace falta.</summary>
    public void Revisar()
    {
        LeerCacheUnaVez();
        var datos = Actual;
        if (datos is not null && (DateTime.Now - datos.Generado).TotalHours < _op.RefrescoHoras) return;
        lock (_cerrojo)
        {
            if (_carga is { IsCompleted: false }) return;
            if (DateTime.UtcNow - _ultimoFallo < ReintentoTrasFallo) return;
        }
        _log.LogInformation("Sectores: recarga ({Motivo})", datos is null ? "sin datos" : "datos de más de " + _op.RefrescoHoras + " h");
        LanzarCarga();
    }

    /// <summary>«Actualizar ahora». Falso si ya hay una carga en marcha o acaba de pedirse.</summary>
    public bool PedirActualizacion()
    {
        lock (_cerrojo)
        {
            if (_carga is { IsCompleted: false } || (DateTime.UtcNow - _ultimaPeticion).TotalSeconds < MinSegundosEntreRecargas)
            {
                return false;
            }
            _ultimaPeticion = DateTime.UtcNow;
            _ultimoFallo = DateTime.MinValue;
        }
        LanzarCarga();
        return true;
    }

    private void LanzarCarga()
    {
        lock (_cerrojo)
        {
            if (_carga is { IsCompleted: false }) return;
            _carga = Task.Run(CargarAsync);
        }
    }

    private async Task CargarAsync()
    {
        try
        {
            var datos = await _fuente.TraerAsync(CancellationToken.None);
            Actual = datos;
            UltimoError = null;
            GuardarCache(datos);
            _log.LogInformation("Sectores: {Filas} filas ({Avisos} avisos)", datos.Filas.Count, datos.Avisos.Count);
        }
        catch (Exception ex)
        {
            UltimoError = ex.Message;
            lock (_cerrojo) { _ultimoFallo = DateTime.UtcNow; }
            _log.LogError(ex, "Sectores: la carga falló");
        }
    }

    private void LeerCacheUnaVez()
    {
        lock (_cerrojo)
        {
            if (_cacheLeida) return;
            _cacheLeida = true;
        }
        try
        {
            if (!File.Exists(_rutaCache)) return;
            var datos = JsonSerializer.Deserialize<DatosSectores>(File.ReadAllText(_rutaCache));
            if (datos is { Version: DatosSectores.VersionActual }) Actual ??= datos;
        }
        catch (Exception ex)
        {
            _log.LogWarning("Sectores: la caché no se pudo leer y se vuelve a traer: {Error}", ex.Message);
        }
    }

    private void GuardarCache(DatosSectores datos)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_rutaCache)!);
            var temporal = _rutaCache + ".tmp";
            File.WriteAllText(temporal, JsonSerializer.Serialize(datos));
            File.Move(temporal, _rutaCache, overwrite: true);
        }
        catch (Exception ex)
        {
            _log.LogWarning("Sectores: no se pudo guardar la caché: {Error}", ex.Message);
        }
    }
}

/// <summary>Mira cada 30 minutos si los datos son viejos y, si lo son, los vuelve a traer.</summary>
public sealed class RevisionSectores : BackgroundService
{
    private readonly ServicioSectores _sectores;

    public RevisionSectores(ServicioSectores sectores) => _sectores = sectores;

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            _sectores.Revisar();
            try { await Task.Delay(TimeSpan.FromMinutes(30), ct); }
            catch (OperationCanceledException) { break; }
        }
    }
}
