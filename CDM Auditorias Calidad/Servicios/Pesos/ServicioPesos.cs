using System.Text.Json;
using CDM_Auditorias_Calidad.Servicios.Configuracion;
using Microsoft.Extensions.Options;

namespace CDM_Auditorias_Calidad.Servicios.Pesos;

/// <summary>Lo leído de un fichero: el fichero elegido, cómo fue y sus bloques de objetivos.</summary>
/// <param name="Estado"><c>ok</c>, <c>sin-bloques</c>, <c>xlsb</c> (no se puede leer) o <c>error</c>.</param>
public sealed record ArchivoAnalizado(ArchivoElegido Archivo, string Estado, string? Error, List<BloquePesos> Bloques);

/// <summary>El análisis de un mes: los ficheros leídos y cuándo.</summary>
public sealed class AnalisisPesos
{
    public string Mes { get; set; } = "";
    public DateTime Analizado { get; set; }
    public List<ArchivoAnalizado> Archivos { get; set; } = new();
}

/// <summary>Todos los meses analizados: se guarda entero en la caché.</summary>
public sealed class DatosPesos
{
    public Dictionary<string, AnalisisPesos> Meses { get; set; } = new();
    public int Version { get; set; } = VersionActual;
    public const int VersionActual = 1;
}

/// <summary>
/// «Pesos y metas por sector»: lee los Excel de ranking de un mes (la versión más reciente de cada sector) y guarda sus
/// bloques de objetivos. Solo se analiza al pedirlo («Analizar») o la primera vez que se abre un mes sin analizar: leer
/// 40 ficheros de la red tarda un par de minutos.
/// </summary>
public sealed class ServicioPesos
{
    private readonly OpcionesPesos _op;
    private readonly string _rutaCache;
    private readonly ILogger<ServicioPesos> _log;
    private readonly object _cerrojo = new();
    private DatosPesos _datos = new();
    private bool _cacheLeida;
    private Task? _analisis;

    public ServicioPesos(IOptions<OpcionesPesos> opciones, IWebHostEnvironment entorno, ILogger<ServicioPesos> log)
    {
        _op = opciones.Value;
        _log = log;
        _rutaCache = Path.IsPathRooted(_op.RutaCache) ? _op.RutaCache : Path.GetFullPath(Path.Combine(entorno.ContentRootPath, _op.RutaCache));
    }

    public string Raiz => _op.RutaRaiz;

    /// <summary>La ruta de un fichero como la abre el usuario (<c>Y:\…</c> en vez de la ruta de red).</summary>
    public string RutaVisible(string ruta)
        => !string.IsNullOrWhiteSpace(_op.RutaVisible) && ruta.StartsWith(_op.RutaRaiz, StringComparison.OrdinalIgnoreCase)
            ? _op.RutaVisible.TrimEnd('\\') + ruta[_op.RutaRaiz.TrimEnd('\\').Length..]
            : ruta;

    /// <summary>El mes que se está analizando ahora y por dónde va («12 de 38: CO Retención»), o nulo.</summary>
    public string? MesEnCurso { get; private set; }
    public string? Progreso { get; private set; }

    /// <summary>El último error al listar las carpetas o al analizar (se borra al analizar bien).</summary>
    public string? UltimoError { get; private set; }

    public bool Analizando
    {
        get { lock (_cerrojo) { return _analisis is { IsCompleted: false }; } }
    }

    /// <summary>Las carpetas de mes con ranking (de la más reciente a la más antigua). Vacío si no se llega a la carpeta.</summary>
    public List<MesIncentivos> Meses()
    {
        try
        {
            return ArchivosRanking.MesesDisponibles(_op.RutaRaiz);
        }
        catch (Exception ex)
        {
            UltimoError = $"No se pudo leer la carpeta {_op.RutaRaiz}: {ex.Message}";
            return [];
        }
    }

    /// <summary>El último análisis guardado (de cualquier mes), sin tocar la red: para la portada.</summary>
    public AnalisisPesos? UltimoAnalisis()
    {
        LeerCacheUnaVez();
        lock (_cerrojo) { return _datos.Meses.Values.OrderByDescending(a => a.Analizado).FirstOrDefault(); }
    }

    public AnalisisPesos? Analisis(string mes)
    {
        LeerCacheUnaVez();
        lock (_cerrojo) { return _datos.Meses.TryGetValue(mes, out var a) ? a : null; }
    }

    /// <summary>Lanza el análisis de un mes. Falso si ya hay uno en marcha.</summary>
    public bool Analizar(MesIncentivos mes)
    {
        lock (_cerrojo)
        {
            if (_analisis is { IsCompleted: false }) return false;
            MesEnCurso = mes.Clave;
            Progreso = "buscando los ficheros";
            _analisis = Task.Run(() => AnalizarAsync(mes));
        }
        return true;
    }

    private Task AnalizarAsync(MesIncentivos mes)
    {
        try
        {
            var elegidos = ArchivosRanking.Elegir(mes);
            var archivos = new List<ArchivoAnalizado>();
            for (var i = 0; i < elegidos.Count; i++)
            {
                var a = elegidos[i];
                Progreso = $"{i + 1} de {elegidos.Count}: {a.Sector}";
                archivos.Add(Leer(a));
            }
            var analisis = new AnalisisPesos { Mes = mes.Clave, Analizado = DateTime.Now, Archivos = archivos };
            lock (_cerrojo) { _datos.Meses[mes.Clave] = analisis; }
            UltimoError = null;
            GuardarCache();
            _log.LogInformation("Pesos {Mes}: {Ficheros} ficheros, {Bloques} bloques", mes.Clave, archivos.Count, archivos.Sum(x => x.Bloques.Count));
        }
        catch (Exception ex)
        {
            UltimoError = ex.Message;
            _log.LogError(ex, "Pesos {Mes}: el análisis falló", mes.Clave);
        }
        finally
        {
            MesEnCurso = null;
            Progreso = null;
        }
        return Task.CompletedTask;
    }

    private ArchivoAnalizado Leer(ArchivoElegido a)
    {
        if (a.Ruta.EndsWith(".xlsb", StringComparison.OrdinalIgnoreCase))
        {
            return new ArchivoAnalizado(a, "xlsb", "Es un .xlsb (libro binario) y no se puede leer: guárdalo como .xlsm o .xlsx.", []);
        }
        try
        {
            var bloques = LectorCabeceras.Leer(a.Ruta, _op.Filas, _op.Columnas).SelectMany(ExtractorPesos.Extraer).ToList();
            return new ArchivoAnalizado(a, bloques.Count > 0 ? "ok" : "sin-bloques",
                bloques.Count > 0 ? null : "No se encontró ningún bloque de objetivos (cabecera 0 | 1 | 1,5) en las primeras filas.", bloques);
        }
        catch (Exception ex)
        {
            _log.LogWarning("Pesos: no se pudo leer {Ruta}: {Error}", a.Ruta, ex.Message);
            return new ArchivoAnalizado(a, "error", ex.Message, []);
        }
    }

    private void LeerCacheUnaVez()
    {
        lock (_cerrojo)
        {
            if (_cacheLeida) return;
            _cacheLeida = true;
            try
            {
                if (!File.Exists(_rutaCache)) return;
                var d = JsonSerializer.Deserialize<DatosPesos>(File.ReadAllText(_rutaCache));
                if (d is { Version: DatosPesos.VersionActual }) _datos = d;
            }
            catch (Exception ex)
            {
                _log.LogWarning("Pesos: la caché no se pudo leer: {Error}", ex.Message);
            }
        }
    }

    private void GuardarCache()
    {
        try
        {
            string texto;
            lock (_cerrojo) { texto = JsonSerializer.Serialize(_datos); }
            Directory.CreateDirectory(Path.GetDirectoryName(_rutaCache)!);
            File.WriteAllText(_rutaCache + ".tmp", texto);
            File.Move(_rutaCache + ".tmp", _rutaCache, overwrite: true);
        }
        catch (Exception ex)
        {
            _log.LogWarning("Pesos: no se pudo guardar la caché: {Error}", ex.Message);
        }
    }
}
