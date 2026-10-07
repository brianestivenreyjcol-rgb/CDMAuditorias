using System.Globalization;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;

namespace CDM_Auditorias_Calidad.Servicios.Pesos;

/// <summary>Las primeras filas de una hoja: <c>Celdas[fila][columna]</c>, con el valor guardado (texto o número) o nulo.</summary>
public sealed record CabeceraHoja(string Hoja, object?[][] Celdas);

/// <summary>
/// Lee solo la esquina de arriba de cada hoja de un .xlsx / .xlsm, sin cargar el libro entero: los Excel de ranking pesan
/// hasta 20 MB y el bloque de objetivos está siempre en las primeras filas. Usa el valor guardado de cada celda (el
/// último cálculo de Excel), no la fórmula.
/// </summary>
/// <remarks>Los .xlsb (binarios) no se pueden leer así: el que llama los marca como no legibles.</remarks>
public static class LectorCabeceras
{
    public static List<CabeceraHoja> Leer(string ruta, int filas, int columnas)
    {
        // FileShare.ReadWrite: el Excel puede estar abierto por quien lo prepara.
        using var flujo = new FileStream(ruta, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
        using var libro = SpreadsheetDocument.Open(flujo, false);
        var parte = libro.WorkbookPart ?? throw new InvalidDataException("El fichero no tiene libro.");
        var compartidas = LeerCompartidas(parte.SharedStringTablePart);
        var salida = new List<CabeceraHoja>();
        foreach (var hoja in parte.Workbook.Sheets?.Elements<Sheet>() ?? [])
        {
            if (hoja.Id?.Value is not { } id || parte.GetPartById(id) is not WorksheetPart ws) continue;
            salida.Add(new CabeceraHoja(hoja.Name?.Value ?? "", LeerHoja(ws, compartidas, filas, columnas)));
        }
        return salida;
    }

    private static List<string> LeerCompartidas(SharedStringTablePart? parte)
    {
        var lista = new List<string>();
        if (parte is null) return lista;
        using var r = OpenXmlReader.Create(parte);
        while (r.Read())
        {
            if (r.ElementType == typeof(SharedStringItem) && r.IsStartElement)
            {
                lista.Add(((SharedStringItem)r.LoadCurrentElement()!).InnerText);
            }
        }
        return lista;
    }

    private static object?[][] LeerHoja(WorksheetPart ws, List<string> compartidas, int filas, int columnas)
    {
        var celdas = new object?[filas][];
        for (var i = 0; i < filas; i++) celdas[i] = new object?[columnas];
        using var r = OpenXmlReader.Create(ws);
        while (r.Read())
        {
            if (r.ElementType == typeof(Row) && r.IsStartElement)
            {
                var indice = r.Attributes.FirstOrDefault(a => a.LocalName == "r").Value;
                if (uint.TryParse(indice, out var n) && n > filas) break;   // ya pasó la zona que interesa: no se lee el resto
            }
            if (r.ElementType != typeof(Cell) || !r.IsStartElement) continue;
            var c = (Cell)r.LoadCurrentElement()!;
            if (c.CellReference?.Value is not { } refe) continue;
            var (fila, col) = Posicion(refe);
            if (fila < 1 || fila > filas || col < 1 || col > columnas) continue;
            celdas[fila - 1][col - 1] = Valor(c, compartidas);
        }
        return celdas;
    }

    private static object? Valor(Cell c, List<string> compartidas)
    {
        var tipo = c.DataType?.Value;
        if (tipo == CellValues.InlineString) return c.InlineString?.InnerText;
        var texto = c.CellValue?.Text;
        if (texto is null) return null;
        if (tipo == CellValues.SharedString)
        {
            return int.TryParse(texto, out var i) && i >= 0 && i < compartidas.Count ? compartidas[i] : null;
        }
        if (tipo == CellValues.String || tipo == CellValues.Error) return texto;
        if (tipo == CellValues.Boolean) return texto == "1";
        return double.TryParse(texto, NumberStyles.Float, CultureInfo.InvariantCulture, out var d) ? d : texto;
    }

    /// <summary>«AB12» → (12, 28).</summary>
    public static (int Fila, int Columna) Posicion(string referencia)
    {
        int col = 0, i = 0;
        for (; i < referencia.Length && char.IsLetter(referencia[i]); i++) col = col * 26 + (char.ToUpperInvariant(referencia[i]) - 'A' + 1);
        return (int.TryParse(referencia.AsSpan(i), out var fila) ? fila : 0, col);
    }
}
