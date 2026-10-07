namespace CDM_Auditorias_Calidad.Servicios.Configuracion;

/// <summary>
/// Sección <c>Pesos</c> de appsettings.json: «Pesos y metas por sector», leídos de los Excel de ranking de incentivos.
/// </summary>
public sealed class OpcionesPesos
{
    public const string Seccion = "Pesos";

    /// <summary>
    /// La carpeta de incentivos (ruta UNC: la unidad <c>Y:</c> es del usuario). Dentro, un año por carpeta, un mes por
    /// subcarpeta («09. SEPTIEMBRE») y en cada mes <c>01. RANKING</c>.
    /// </summary>
    public string RutaRaiz { get; set; } = @"\\172.16.232.102\incentivos\INCENTIVOS JAZZPLAT";

    /// <summary>
    /// La misma carpeta como la abre el usuario (con su unidad <c>Y:</c>): es la ruta que enseña la vista para copiarla y
    /// abrir el fichero. Vacío: se enseña <see cref="RutaRaiz"/>.
    /// </summary>
    public string RutaVisible { get; set; } = @"Y:\INCENTIVOS JAZZPLAT";

    /// <summary>Lo analizado, en disco. Relativo a la carpeta de la aplicación.</summary>
    public string RutaCache { get; set; } = Path.Combine("App_Data", "cache_pesos.json");

    /// <summary>Filas y columnas de cada hoja en las que se busca el bloque de objetivos.</summary>
    public int Filas { get; set; } = 40;
    public int Columnas { get; set; } = 200;
}
