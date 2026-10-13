namespace CDM_Auditorias_Calidad.Models;

/// <summary>Cuándo se trajeron los datos de un informe y si se están trayendo ahora.</summary>
public sealed record EstadoInforme(DateTime? Cargado, bool EnCurso);

/// <summary>
/// El grupo «General» de la portada (<c>/informes</c>): si hay datos de cada informe y de cuándo son. Auditorías trae su
/// instantánea entera (para el número de auditorías y el rango); No solución y GAIA, solo su estado.
/// </summary>
public sealed record MenuModelo(InstantaneaAuditorias? Datos, string? Error, EstadoInforme NoSolucion, EstadoInforme Gaia);

/// <summary>El grupo «Personal» de la portada (<c>/personal</c>, con contraseña): el estado de Sectores y de Pesos.</summary>
public sealed record PersonalModelo(EstadoInforme Sectores, EstadoInforme Pesos);

/// <summary>
/// La portada: las dos tarjetas de grupo. De «General», cuántos de sus tres informes tienen ya datos; de «Personal», si
/// este navegador ya tiene el pase de la contraseña.
/// </summary>
public sealed record PortadaModelo(int GeneralConDatos, bool PersonalAbierto, bool PersonalConfigurado);

/// <summary>La página de la contraseña de «Personal».</summary>
public sealed record EntrarModelo(string Volver, string? Error);
