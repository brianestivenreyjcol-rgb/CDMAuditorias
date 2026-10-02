namespace CDM_Auditorias_Calidad.Servicios.Tablero;

/// <summary>
/// Una de las dos páginas del Power BI con el tablero: «General» y «Formación &amp; Calidad».
/// </summary>
/// <param name="Clave">Ruta de la página (<c>/general</c>, <c>/formacion</c>).</param>
/// <param name="VistaInicial">Con qué agrupa los gráficos al entrar (la que dejó guardada el PBI).</param>
/// <param name="Cargos">Filtro de página sobre Cargo_Auditor; null = sin filtro.</param>
public sealed record PaginaTablero(string Clave, string Titulo, Vista VistaInicial, IReadOnlySet<string>? Cargos)
{
    public const string ClaveGeneral = "general";
    public const string ClaveFormacion = "formacion";

    public static PaginaTablero General { get; } = new(ClaveGeneral, "General", Vista.Semana, null);

    public static PaginaTablero Formacion(IEnumerable<string> cargos)
        => new(ClaveFormacion, "Formación & Calidad", Vista.Mes, new HashSet<string>(cargos, StringComparer.Ordinal));
}
