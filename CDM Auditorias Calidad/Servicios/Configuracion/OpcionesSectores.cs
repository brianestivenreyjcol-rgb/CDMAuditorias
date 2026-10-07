namespace CDM_Auditorias_Calidad.Servicios.Configuracion;

/// <summary>
/// Sección <c>Sectores</c> de appsettings.json: «Rellamada y No solución por sector», la cifra mensual de
/// cada sector de Bogotá. YGMM sale de BigQuery y Jazztel, Orange y WhatsApp, de SQL Server (las mismas
/// credenciales del .env que Auditorías: <see cref="OpcionesAuditorias.RutaEnv"/>).
/// </summary>
public sealed class OpcionesSectores
{
    public const string Seccion = "Sectores";

    /// <summary>Cadena ODBC del driver Simba de BigQuery (el mismo DSN que No solución y GAIA).</summary>
    public string Odbc { get; set; } = "DSN=BQCOL;";

    /// <summary>Los datos ya traídos, en disco. Relativo a la carpeta de la aplicación.</summary>
    public string RutaCache { get; set; } = Path.Combine("App_Data", "cache_sectores.json");

    /// <summary>Horas tras las que se vuelven a traer (las tablas de SQL se cargan una vez al día).</summary>
    public double RefrescoHoras { get; set; } = 6;

    /// <summary>Tiempo máximo de cada consulta a SQL Server.</summary>
    public int SegundosConsulta { get; set; } = 300;
}
