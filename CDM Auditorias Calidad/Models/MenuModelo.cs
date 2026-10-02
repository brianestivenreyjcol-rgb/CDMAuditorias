namespace CDM_Auditorias_Calidad.Models;

/// <summary>La portada: solo necesita saber si hay datos y de cuándo son.</summary>
public sealed record MenuModelo(InstantaneaAuditorias? Datos, string? Error);
