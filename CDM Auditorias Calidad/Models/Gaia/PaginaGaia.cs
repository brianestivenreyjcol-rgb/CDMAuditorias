using CDM_Auditorias_Calidad.Servicios.Gaia;
using Microsoft.AspNetCore.WebUtilities;

namespace CDM_Auditorias_Calidad.Models.Gaia;

/// <summary>Las pestañas de GAIA Formación (las páginas del PBI que ya están en la web).</summary>
public enum PestanaGaia { Resumen, Ranking, Estilo, Rendimiento, Evolucion, Comercial, Motivos, Espanolizacion, Llamadas }

/// <summary>
/// Lo común a todas las páginas de GAIA Formación: estado de los datos, filtros resueltos y enlaces.
/// Sin datos todavía (<see cref="Preparando"/>), las vistas enseñan «preparando» y no hay filtros.
/// </summary>
public abstract class PaginaGaia
{
    public const string RutaBase = "/gaia";

    public abstract PestanaGaia Pestana { get; }

    /// <summary>Nulo mientras no hay datos.</summary>
    public FiltrosResueltosGaia? Filtros { get; init; }
    public DatosGaia? Datos { get; init; }

    /// <summary>Se están trayendo los datos ahora (primera vez, Excel cambiado o «Actualizar»).</summary>
    public bool Cargando { get; init; }

    /// <summary>El último error al leer el Excel o al consultar BigQuery.</summary>
    public string? Error { get; init; }

    /// <summary>Resultado de pulsar «Actualizar»: <c>en-curso</c> o <c>ya-en-curso</c>.</summary>
    public string? Actualizacion { get; init; }

    public bool Preparando => Filtros is null;

    /// <summary>Indicadores de todas las llamadas filtradas (las tarjetas de arriba de cada página).</summary>
    public IndicadoresGaia? Total { get; init; }

    /// <summary>Avisos del Excel (filas sin ID, repetidos…).</summary>
    public IReadOnlyList<string> AvisosExcel => Datos?.AvisosExcel ?? [];

    /// <summary>Lo que no se pudo traer en la última carga (p. ej. la españolización).</summary>
    public IReadOnlyList<string> AvisosCarga => Datos?.AvisosCarga ?? [];

    /// <summary>Parámetros propios de la pestaña que se conservan al cambiar los filtros (orden, KPI…).</summary>
    public virtual IEnumerable<KeyValuePair<string, string>> ParametrosPropios => [];

    public static string Ruta(PestanaGaia p) => p switch
    {
        PestanaGaia.Ranking => RutaBase + "/ranking",
        PestanaGaia.Estilo => RutaBase + "/estilo",
        PestanaGaia.Rendimiento => RutaBase + "/rendimiento",
        PestanaGaia.Evolucion => RutaBase + "/evolucion",
        PestanaGaia.Comercial => RutaBase + "/comercial",
        PestanaGaia.Motivos => RutaBase + "/motivos",
        PestanaGaia.Espanolizacion => RutaBase + "/espanolizacion",
        PestanaGaia.Llamadas => RutaBase + "/llamadas",
        _ => RutaBase,
    };

    public static string Titulo(PestanaGaia p) => p switch
    {
        PestanaGaia.Ranking => "Ranking",
        PestanaGaia.Estilo => "Estilo",
        PestanaGaia.Rendimiento => "Rendimiento",
        PestanaGaia.Evolucion => "Evolución",
        PestanaGaia.Comercial => "Comercial",
        PestanaGaia.Motivos => "Motivos",
        PestanaGaia.Espanolizacion => "Españolización",
        PestanaGaia.Llamadas => "Llamadas",
        _ => "Resumen",
    };

    /// <summary>Enlace a otra pestaña (o a esta) con los mismos filtros.</summary>
    public string UrlPestana(PestanaGaia p) => ConParametros(Ruta(p), Filtros?.Parametros() ?? []);

    /// <summary>Esta pestaña con los filtros, cambiando o quitando parámetros propios (valor nulo = quitar).</summary>
    public string UrlCon(params (string Clave, string? Valor)[] cambios)
    {
        var propios = ParametrosPropios.Where(p => cambios.All(c => c.Clave != p.Key))
            .Concat(cambios.Where(c => c.Valor is not null).Select(c => new KeyValuePair<string, string>(c.Clave, c.Valor!)));
        return ConParametros(Ruta(Pestana), (Filtros?.Parametros() ?? []).Concat(propios));
    }

    /// <summary>Esta pestaña sin filtros (conserva los parámetros propios).</summary>
    public string UrlLimpiar => ConParametros(Ruta(Pestana), ParametrosPropios);

    /// <summary>Descarga en CSV de las llamadas con estos filtros.</summary>
    public string UrlCsv => ConParametros(RutaBase + "/llamadas/csv", Filtros?.Parametros() ?? []);

    protected static string ConParametros(string ruta, IEnumerable<KeyValuePair<string, string>> parametros)
        => QueryHelpers.AddQueryString(ruta, parametros.Select(p => new KeyValuePair<string, string?>(p.Key, p.Value)));
}

/// <summary>Portada de GAIA: KPIs, avance por etapa de formación, evolución y actualización por marca.</summary>
public sealed class PaginaResumenGaia : PaginaGaia
{
    public override PestanaGaia Pestana => PestanaGaia.Resumen;

    /// <summary>El KPI de la gráfica de evolución.</summary>
    public KpiGaia Kpi { get; init; } = CalculadoraGaia.Kpis[0];
    public IReadOnlyList<GrupoGaia> PorEtapa { get; init; } = [];
    public IReadOnlyList<GrupoGaia> PorDia { get; init; } = [];
    public IReadOnlyList<GrupoGaia> PorMarca { get; init; } = [];

    /// <summary>Agentes distintos con llamadas en el filtro.</summary>
    public int AgentesConLlamadas { get; init; }

    /// <summary>Agentes del Excel que entran en el filtro de nómina (tengan llamadas o no).</summary>
    public int AgentesEnNomina { get; init; }

    public override IEnumerable<KeyValuePair<string, string>> ParametrosPropios
        => Kpi.Clave == CalculadoraGaia.Kpis[0].Clave ? [] : [new("kpi", Kpi.Clave)];
}

/// <summary>Ranking de agentes con todos los KPI (página «Ranking» del PBI).</summary>
public sealed class PaginaRankingGaia : PaginaGaia
{
    public override PestanaGaia Pestana => PestanaGaia.Ranking;

    public IReadOnlyList<FilaAgenteGaia> Filas { get; init; } = [];

    /// <summary>Clave de <see cref="CalculadoraGaia.Kpis"/>, «llamadas» o «agente».</summary>
    public string Orden { get; init; } = "adherencia";
    public bool Descendente { get; init; } = true;
    public string? Busqueda { get; init; }

    public override IEnumerable<KeyValuePair<string, string>> ParametrosPropios
    {
        get
        {
            if (Orden != "adherencia") yield return new("orden", Orden);
            if (!Descendente) yield return new("dir", "asc");
            if (!string.IsNullOrEmpty(Busqueda)) yield return new("q", Busqueda);
        }
    }

    /// <summary>Enlace para ordenar por <paramref name="clave"/> (si ya es esa, cambia el sentido).</summary>
    public string UrlOrden(string clave)
    {
        var desc = clave == Orden ? !Descendente : clave != "agente";
        return UrlCon(("orden", clave == "adherencia" ? null : clave), ("dir", desc ? null : "asc"));
    }
}

/// <summary>Adherencia al estilo y sus criterios (páginas «Estilo» y «Estilo Tabla» del PBI).</summary>
public sealed class PaginaEstiloGaia : PaginaGaia
{
    public override PestanaGaia Pestana => PestanaGaia.Estilo;

    public IReadOnlyList<GrupoGaia> PorEtapa { get; init; } = [];
    public IReadOnlyList<GrupoGaia> PorDia { get; init; } = [];

    /// <summary>Agentes ordenados de menor a mayor adherencia (los que más ayuda necesitan, arriba).</summary>
    public IReadOnlyList<FilaAgenteGaia> Agentes { get; init; } = [];

    /// <summary>Adherencia de cada agente en cada etapa: [IdAgente][etapa].</summary>
    public IReadOnlyDictionary<string, IReadOnlyDictionary<string, double?>> MatrizEtapas { get; init; }
        = new Dictionary<string, IReadOnlyDictionary<string, double?>>();

    /// <summary>Las etapas que tienen llamadas, en orden (columnas de la matriz).</summary>
    public IReadOnlyList<string> Etapas { get; init; } = [];

    /// <summary>Umbrales de color de la barra de adherencia del PBI.</summary>
    public const double UmbralBajo = 0.40, UmbralMedio = 0.70;
}

/// <summary>Las llamadas una a una (página «Base» del PBI), paginadas.</summary>
public sealed class PaginaLlamadasGaia : PaginaGaia
{
    public const int PorPagina = 100;

    public override PestanaGaia Pestana => PestanaGaia.Llamadas;

    public IReadOnlyList<LlamadaGaia> Filas { get; init; } = [];
    public int TotalFilas { get; init; }
    public int Pagina { get; init; } = 1;
    public string? Busqueda { get; init; }

    public int TotalPaginas => Math.Max(1, (TotalFilas + PorPagina - 1) / PorPagina);

    public override IEnumerable<KeyValuePair<string, string>> ParametrosPropios
    {
        get
        {
            if (!string.IsNullOrEmpty(Busqueda)) yield return new("q", Busqueda);
            if (Pagina > 1) yield return new("pagina", Pagina.ToString());
        }
    }

    public string UrlPagina(int pagina) => UrlCon(("pagina", pagina > 1 ? pagina.ToString() : null));
}

/// <summary>Tiempos y llamadas cortas o sin contexto (página «Rendimiento» del PBI).</summary>
public sealed class PaginaRendimientoGaia : PaginaGaia
{
    public override PestanaGaia Pestana => PestanaGaia.Rendimiento;

    public IReadOnlyList<GrupoGaia> PorDia { get; init; } = [];
    public IReadOnlyList<GrupoGaia> PorSemana { get; init; } = [];
    public IReadOnlyList<GrupoGaia> PorEtapa { get; init; } = [];

    /// <summary>Agentes de mayor a menor TMO.</summary>
    public IReadOnlyList<FilaAgenteGaia> Agentes { get; init; } = [];
}

/// <summary>
/// Un KPI día a día y los agentes enfrentados en dos KPI (páginas «Cronológico» y «Comparativo» del PBI).
/// </summary>
public sealed class PaginaEvolucionGaia : PaginaGaia
{
    public const string KpiPorDefecto = "rellamada", XPorDefecto = "adherencia", YPorDefecto = "rellamada";

    /// <summary>Agentes con menos llamadas no salen en la dispersión.</summary>
    public const int MinLlamadas = 10;

    public override PestanaGaia Pestana => PestanaGaia.Evolucion;

    /// <summary>El KPI de la línea por día y por semana.</summary>
    public KpiGaia Kpi { get; init; } = CalculadoraGaia.Kpi(KpiPorDefecto);

    /// <summary>Ejes de la dispersión de agentes.</summary>
    public KpiGaia KpiX { get; init; } = CalculadoraGaia.Kpi(XPorDefecto);
    public KpiGaia KpiY { get; init; } = CalculadoraGaia.Kpi(YPorDefecto);

    public IReadOnlyList<GrupoGaia> PorDia { get; init; } = [];
    public IReadOnlyList<GrupoGaia> PorSemana { get; init; } = [];

    /// <summary>Los agentes con al menos <see cref="MinLlamadas"/> llamadas (los puntos de la dispersión).</summary>
    public IReadOnlyList<FilaAgenteGaia> Agentes { get; init; } = [];

    public override IEnumerable<KeyValuePair<string, string>> ParametrosPropios
    {
        get
        {
            if (Kpi.Clave != KpiPorDefecto) yield return new("kpi", Kpi.Clave);
            if (KpiX.Clave != XPorDefecto) yield return new("x", KpiX.Clave);
            if (KpiY.Clave != YPorDefecto) yield return new("y", KpiY.Clave);
        }
    }
}

/// <summary>Ofrecimientos, ventas y alineación (páginas «Ventas», «Detalle Comercial» y «Alineamientos» del PBI).</summary>
public sealed class PaginaComercialGaia : PaginaGaia
{
    public override PestanaGaia Pestana => PestanaGaia.Comercial;

    /// <summary>Resultado de las llamadas con intento de venta.</summary>
    public IReadOnlyList<RepartoGaia> ResultadosVenta { get; init; } = [];

    /// <summary>Qué se ofreció, en las llamadas con intento de venta.</summary>
    public IReadOnlyList<RepartoGaia> CategoriasOferta { get; init; } = [];

    /// <summary>Qué se vendió, en las llamadas con venta.</summary>
    public IReadOnlyList<RepartoGaia> TiposServicioVenta { get; init; } = [];

    public IReadOnlyList<GrupoGaia> PorDia { get; init; } = [];
    public IReadOnlyList<GrupoGaia> PorEtapa { get; init; } = [];

    /// <summary>Agentes de más a menos ofrecimientos.</summary>
    public IReadOnlyList<FilaAgenteGaia> Agentes { get; init; } = [];
}

/// <summary>Motivos de contacto y cómo afectan a los KPI (páginas «Motivos de Contacto» y «Mapa de afectación» del PBI).</summary>
public sealed class PaginaMotivosGaia : PaginaGaia
{
    public override PestanaGaia Pestana => PestanaGaia.Motivos;

    /// <summary>Motivo 1 → 2 → 3 con su peso y sus indicadores.</summary>
    public IReadOnlyList<NodoMotivo> Arbol { get; init; } = [];

    /// <summary>Nivel de motivo del mapa de afectación (1, 2 o 3).</summary>
    public int Nivel { get; init; } = 2;

    /// <summary>Los motivos del nivel elegido con sus indicadores, de más a menos llamadas.</summary>
    public IReadOnlyList<GrupoGaia> Mapa { get; init; } = [];

    public IReadOnlyList<RepartoGaia> EstadosResolucion { get; init; } = [];
    public IReadOnlyList<RepartoGaia> RazonContacto { get; init; } = [];
    public IReadOnlyList<RepartoGaia> TiposProblema { get; init; } = [];
    public IReadOnlyList<RepartoGaia> Temas { get; init; } = [];

    /// <summary>Obstáculos de la resolución; fracción sobre todas las llamadas.</summary>
    public IReadOnlyList<RepartoGaia> Obstaculos { get; init; } = [];

    public IReadOnlyList<RepartoGaia> SentimientoInicial { get; init; } = [];
    public IReadOnlyList<RepartoGaia> SentimientoFinal { get; init; } = [];

    public override IEnumerable<KeyValuePair<string, string>> ParametrosPropios
        => Nivel == 2 ? [] : [new("nivel", Nivel.ToString())];
}

/// <summary>
/// Españolización: si el agente habla como en España o como en Colombia (páginas «Españolización» y sus
/// niveles Novato, Aficionado y Experto del PBI). Solo cuenta lo que dice el agente.
/// </summary>
public sealed class PaginaEspanolizacionGaia : PaginaGaia
{
    /// <summary>Por debajo de esto, el porcentaje de un agente no es fiable.</summary>
    public const int MinBase = 10;

    public override PestanaGaia Pestana => PestanaGaia.Espanolizacion;

    /// <summary>0 = todas las palabras; 1 Novato, 2 Aficionado, 3 Experto (los pares se acumulan).</summary>
    public int Nivel { get; init; }
    public IReadOnlyList<string> Niveles { get; init; } = [];

    public EspanolizacionGaia? Resumen { get; init; }
    public IReadOnlyList<FilaParGaia> Pares { get; init; } = [];
    public IReadOnlyList<FilaPalabraGaia> Palabras { get; init; } = [];
    public IReadOnlyList<GrupoEspanolizacion> PorEtapa { get; init; } = [];
    public IReadOnlyList<GrupoEspanolizacion> PorDia { get; init; } = [];

    /// <summary>Agentes de menos a más españolización (los que más ayuda necesitan, arriba).</summary>
    public IReadOnlyList<GrupoEspanolizacion> Agentes { get; init; } = [];

    public string NombreNivel(int nivel) => nivel >= 1 && nivel <= Niveles.Count ? Niveles[nivel - 1] : "Todas";

    public override IEnumerable<KeyValuePair<string, string>> ParametrosPropios
        => Nivel == 0 ? [] : [new("nivel", Nivel.ToString())];
}
