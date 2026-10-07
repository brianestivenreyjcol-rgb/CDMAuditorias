namespace CDM_Auditorias_Calidad.Servicios.Sectores;

/// <summary>
/// Un sector en un mes para un indicador: la base (llamadas con dato o encuestas) y los casos (rellamadas o
/// no solucionadas). El porcentaje es siempre <see cref="Casos"/> ÷ <see cref="Base"/>.
/// </summary>
/// <param name="Indicador"><c>rellamada</c> o <c>nosolucion</c> (<see cref="IndicadorSectores.Clave"/>).</param>
/// <param name="Mes">«2026-09».</param>
/// <param name="Marca"><c>YGMM</c>, <c>JAZZTEL</c> u <c>ORANGE</c>.</param>
/// <param name="Fuente">«BigQuery» o «SQL Server».</param>
public sealed record FilaSector(string Indicador, string Mes, string Sector, string Marca, string Fuente, long Base, long Casos);

/// <summary>Todo lo que se trae de una vez: se guarda entero en la caché.</summary>
public sealed class DatosSectores
{
    public List<FilaSector> Filas { get; set; } = new();

    /// <summary>Lo que no se pudo traer (p. ej. BigQuery caído): el resto se enseña igual, con el aviso.</summary>
    public List<string> Avisos { get; set; } = new();

    /// <summary>Cuándo se trajeron.</summary>
    public DateTime Generado { get; set; }

    /// <summary>Versión del formato de la caché: si cambia, la caché vieja no se usa.</summary>
    public int Version { get; set; } = VersionActual;

    // 2 (07-10-2026): CO Técnico Convergente Orange y la no solución de CO Whatsapp JZZ.
    public const int VersionActual = 2;
}

/// <summary>Los dos indicadores del informe y cómo se leen.</summary>
/// <param name="DiasCierre">Días tras el fin de mes hasta que el mes está completo (la rellamada mira 72 h hacia delante).</param>
public sealed record IndicadorSectores(string Clave, string Titulo, string NombreBase, string NombreCasos, int DiasCierre, string Formula)
{
    public static readonly IndicadorSectores Rellamada = new(
        "rellamada", "Rellamada 72 h", "Llamadas con dato", "Rellamadas", 3,
        "Llamadas que se rellamaron en las 72 horas siguientes ÷ llamadas con el dato de rellamada");

    public static readonly IndicadorSectores NoSolucion = new(
        "nosolucion", "No solución", "Encuestas", "No solucionadas", 0,
        "Encuestas «no se solucionó» ÷ encuestas respondidas (sí + no)");

    public static readonly IReadOnlyList<IndicadorSectores> Todos = [Rellamada, NoSolucion];

    public static IndicadorSectores De(string? clave) => Todos.FirstOrDefault(i => i.Clave == clave) ?? Rellamada;
}
