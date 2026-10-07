using System.Globalization;
using CDM_Auditorias_Calidad.Models.Pesos;
using CDM_Auditorias_Calidad.Servicios.Comun;
using CDM_Auditorias_Calidad.Servicios.Pesos;
using Microsoft.AspNetCore.Mvc;

namespace CDM_Auditorias_Calidad.Controllers;

/// <summary>
/// «Pesos y metas por sector»: los bloques de objetivos de los Excel de ranking de incentivos de cada mes, con su
/// validación. «Analizar» vuelve a leer las carpetas; un mes que nunca se analizó se analiza solo al abrirlo.
/// </summary>
[Route(PaginaPesos.Ruta)]
public sealed class PesosController : Controller
{
    private const string ClaveAviso = "PesosAviso";
    private readonly ServicioPesos _pesos;

    public PesosController(ServicioPesos pesos) => _pesos = pesos;

    [HttpGet("")]
    public IActionResult Index([FromQuery] PeticionPesos p)
    {
        var meses = _pesos.Meses();
        var mes = meses.FirstOrDefault(m => m.Clave == p.Mes) ?? meses.FirstOrDefault();
        var analisis = mes is null ? null : _pesos.Analisis(mes.Clave);
        if (mes is not null && analisis is null && !_pesos.Analizando) _pesos.Analizar(mes);

        PaginaPesos modelo;
        if (analisis is null)
        {
            modelo = Modelo(meses, mes, null, p, [], [], [], []);
        }
        else
        {
            var (filas, comprobaciones, desplegables, archivos) = PaginaPesos.Construir(analisis, p, _pesos.RutaVisible, _pesos.HojasExtra);
            modelo = Modelo(meses, mes, analisis, p, filas, comprobaciones, desplegables, archivos);
        }
        ViewData["Parcial"] = Request.Headers["X-Parcial"] == "1";
        return View("Index", modelo);
    }

    [HttpPost("analizar")]
    [ValidateAntiForgeryToken]
    public IActionResult Analizar(string? mes, string? volver)
    {
        var m = _pesos.Meses().FirstOrDefault(x => x.Clave == mes);
        TempData[ClaveAviso] = m is null ? "sin-mes" : _pesos.Analizar(m) ? "en-curso" : "ya-en-curso";
        var destino = !string.IsNullOrEmpty(volver) && Url.IsLocalUrl(volver) && volver.StartsWith(PaginaPesos.Ruta, StringComparison.OrdinalIgnoreCase)
            ? volver : PaginaPesos.Ruta;
        return LocalRedirect(destino);
    }

    /// <summary>La tabla con estos filtros en CSV para Excel.</summary>
    [HttpGet("csv")]
    public IActionResult Csv([FromQuery] PeticionPesos p)
    {
        var meses = _pesos.Meses();
        var mes = meses.FirstOrDefault(m => m.Clave == p.Mes) ?? meses.FirstOrDefault();
        if (mes is null || _pesos.Analisis(mes.Clave) is not { } analisis) return NotFound();
        var (filas, _, _, _) = PaginaPesos.Construir(analisis, p, _pesos.RutaVisible, _pesos.HojasExtra);
        Response.Headers.CacheControl = "private, no-store";
        string N(double? v) => v is { } x ? x.ToString("0.######", CultureInfo.GetCultureInfo("es-ES")) : "";
        var columnas = new List<ColumnaCsv<FilaTablaPesos>>
        {
            new("Carpeta", _ => PaginaPesos.TextoMes(mes)),
            new("Sector", f => f.Sector),
            new("Responsable", f => f.Responsable),
            new("Hoja", f => f.Hoja),
            new("KPI", f => f.Kpi),
            new("Peso", f => N(f.Peso)),
            new("Meta 0%", f => N(f.Meta0)),
            new("Meta 60%", f => N(f.Meta60)),
            new("Meta 100%", f => N(f.Meta100)),
            new("Meta 150%", f => N(f.Meta150)),
            new("Fichero", f => f.Ruta),
        };
        return File(ExportacionCsv.Generar(filas, columnas), ExportacionCsv.TipoContenido, $"pesos_por_sector_{mes.Clave}.csv");
    }

    private PaginaPesos Modelo(List<MesIncentivos> meses, MesIncentivos? mes, AnalisisPesos? analisis, PeticionPesos p,
        List<FilaTablaPesos> filas, List<ComprobacionPesos> comprobaciones, List<Models.GrupoFiltro> desplegables, List<ArchivoAnalizado> archivos)
        => new()
        {
            Meses = meses, Mes = mes, Analisis = analisis, Peticion = p,
            Analizando = _pesos.Analizando, MesEnCurso = _pesos.MesEnCurso, Progreso = _pesos.Progreso, Error = _pesos.UltimoError,
            Aviso = TempData[ClaveAviso] as string, Raiz = _pesos.Raiz, RutaVisible = _pesos.RutaVisible, Extras = _pesos.HojasExtra,
            Filas = filas, Comprobaciones = comprobaciones, Desplegables = desplegables, Archivos = archivos,
        };
}
