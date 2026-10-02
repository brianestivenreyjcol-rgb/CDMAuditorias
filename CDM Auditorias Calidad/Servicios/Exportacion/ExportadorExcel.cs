using ClosedXML.Excel;
using CDM_Auditorias_Calidad.Models;

namespace CDM_Auditorias_Calidad.Servicios.Exportacion;

/// <summary>
/// El «Descargable» del Power BI: el detalle de auditorías filtrado, en Excel.
/// </summary>
/// <remarks>
/// Mismas columnas que la tabla del PBI (Base, Fecha, Super, Team, Agente, Sector, Auditor,
/// Cargo Auditor, Respuesta), pero una fila por auditoría: la tabla del PBI juntaba las filas
/// idénticas y sumaba su nota.
/// </remarks>
public static class ExportadorExcel
{
    public const string TipoContenido = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

    public static byte[] Generar(IReadOnlyList<Auditoria> filas, string titulo)
    {
        using var libro = new XLWorkbook();
        var hoja = libro.AddWorksheet("Auditorias");

        string[] cabeceras = ["Base", "Fecha", "Super", "Team", "Agente", "Sector", "Auditor", "Cargo Auditor", "Respuesta"];
        for (var c = 0; c < cabeceras.Length; c++) hoja.Cell(1, c + 1).Value = cabeceras[c];

        for (var i = 0; i < filas.Count; i++)
        {
            var a = filas[i];
            var r = i + 2;
            hoja.Cell(r, 1).Value = a.Base;
            hoja.Cell(r, 2).Value = a.Fecha.ToDateTime(TimeOnly.MinValue);
            hoja.Cell(r, 3).Value = a.Super;
            hoja.Cell(r, 4).Value = a.Team;
            hoja.Cell(r, 5).Value = a.Agente;
            hoja.Cell(r, 6).Value = a.Sector;
            hoja.Cell(r, 7).Value = a.NombreAuditor;
            hoja.Cell(r, 8).Value = a.CargoAuditor;
            if (a.Respuesta is { } nota) hoja.Cell(r, 9).Value = nota;
        }

        var cabecera = hoja.Range(1, 1, 1, cabeceras.Length);
        cabecera.Style.Font.Bold = true;
        // Cabecera de tabla de la marca Orange (guía de estilos, 2.1).
        cabecera.Style.Font.FontColor = XLColor.FromHtml("#7A3A00");
        cabecera.Style.Fill.BackgroundColor = XLColor.FromHtml("#FFE3CC");

        var ultima = Math.Max(2, filas.Count + 1);
        hoja.Range(2, 2, ultima, 2).Style.NumberFormat.Format = "dd/mm/yyyy";
        hoja.Range(2, 9, ultima, 9).Style.NumberFormat.Format = "0.00 %";

        // Anchos fijos: ajustarlos al contenido con miles de filas es lento.
        double[] anchos = [10, 12, 30, 30, 34, 30, 34, 22, 11];
        for (var c = 0; c < anchos.Length; c++) hoja.Column(c + 1).Width = anchos[c];

        hoja.Range(1, 1, ultima, cabeceras.Length).SetAutoFilter();
        hoja.SheetView.FreezeRows(1);
        libro.Properties.Title = titulo;

        using var flujo = new MemoryStream();
        libro.SaveAs(flujo);
        return flujo.ToArray();
    }
}
