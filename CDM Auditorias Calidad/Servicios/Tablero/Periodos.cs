namespace CDM_Auditorias_Calidad.Servicios.Tablero;

/// <summary>
/// Las funciones de fechas de DAX que usan las medidas del Power BI.
/// </summary>
public static class Periodos
{
    /// <summary>Lunes de la semana de la fecha (<c>fecha - WEEKDAY(fecha, 2) + 1</c>).</summary>
    public static DateOnly Lunes(DateOnly fecha) => fecha.AddDays(-(((int)fecha.DayOfWeek + 6) % 7));

    public static DateOnly InicioDeMes(DateOnly fecha) => new(fecha.Year, fecha.Month, 1);

    public static DateOnly FinDeMes(DateOnly fecha) => new(fecha.Year, fecha.Month, DateTime.DaysInMonth(fecha.Year, fecha.Month));

    /// <summary>
    /// <c>WEEKNUM(fecha, 2)</c>: la semana 1 es la que contiene el 1 de enero y las semanas
    /// empiezan en lunes (columna <c>Calendario[Semana]</c>).
    /// </summary>
    public static int Semana(DateOnly fecha)
    {
        var desfase = ((int)new DateOnly(fecha.Year, 1, 1).DayOfWeek + 6) % 7;
        return (fecha.DayOfYear - 1 + desfase) / 7 + 1;
    }

    /// <summary>
    /// <c>DATEADD(Calendario[Fecha], dias, DAY)</c>, recortado al calendario.
    /// </summary>
    public static SortedSet<DateOnly> DesplazarDias(IEnumerable<DateOnly> fechas, int dias, DateOnly minimo, DateOnly maximo)
        => new(fechas.Select(f => f.AddDays(dias)).Where(f => f >= minimo && f <= maximo));

    /// <summary>
    /// <c>DATEADD(Calendario[Fecha], meses, MONTH)</c>, recortado al calendario.
    /// </summary>
    /// <remarks>
    /// Cada fecha pasa al mismo día del mes de destino (o a su último día, si no existe). Si
    /// la fecha es el último día de su mes, el resultado incluye hasta el último día del mes
    /// de destino: del 1 al 30 de septiembre se pasa al 1–31 de agosto, como en DAX.
    /// </remarks>
    public static SortedSet<DateOnly> DesplazarMeses(IEnumerable<DateOnly> fechas, int meses, DateOnly minimo, DateOnly maximo)
    {
        var resultado = new SortedSet<DateOnly>();
        foreach (var f in fechas)
        {
            var destino = f.AddMonths(meses);
            resultado.Add(destino);
            if (f == FinDeMes(f))
            {
                for (var d = destino.AddDays(1); d <= FinDeMes(destino); d = d.AddDays(1))
                    resultado.Add(d);
            }
        }
        resultado.RemoveWhere(f => f < minimo || f > maximo);
        return resultado;
    }

    /// <summary>
    /// <c>DATESBETWEEN(Calendario[Fecha], desde, hasta)</c>: el intervalo recortado al calendario.
    /// </summary>
    public static (DateOnly Desde, DateOnly Hasta)? Intervalo(DateOnly desde, DateOnly hasta, DateOnly minimo, DateOnly maximo)
    {
        var d = desde < minimo ? minimo : desde;
        var h = hasta > maximo ? maximo : hasta;
        return d <= h ? (d, h) : null;
    }
}
