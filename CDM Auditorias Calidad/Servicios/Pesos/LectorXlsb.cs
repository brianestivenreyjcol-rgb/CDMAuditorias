using System.IO.Compression;
using System.Text;
using System.Xml.Linq;

namespace CDM_Auditorias_Calidad.Servicios.Pesos;

/// <summary>
/// Lee la esquina de arriba de cada hoja de un libro binario de Excel (.xlsb), como <see cref="LectorCabeceras"/> con los .xlsx.
/// En .NET no hay librería para el .xlsb, así que se leen a mano los registros BIFF12 que hacen falta: los nombres de las hojas
/// (<c>xl/workbook.bin</c>), los textos compartidos (<c>xl/sharedStrings.bin</c>) y las celdas (número, texto, booleano y el
/// valor guardado de las fórmulas) hasta la fila que se pida. Lo demás (estilos, fórmulas, formatos) se salta.
/// </summary>
/// <remarks>Formato: [MS-XLSB] de Microsoft. Cada registro lleva su tipo y su largo en 1–2 y 1–4 bytes de 7 bits.</remarks>
public static class LectorXlsb
{
    // Tipos de registro que se usan.
    private const int RowHdr = 0, CellBlank = 1, CellRk = 2, CellError = 3, CellBool = 4, CellReal = 5, CellSt = 6, CellIsst = 7,
        FmlaString = 8, FmlaNum = 9, FmlaBool = 10, FmlaError = 11, SstItem = 19, BundleSh = 156;

    public static List<CabeceraHoja> Leer(string ruta, int filas, int columnas)
    {
        using var flujo = new FileStream(ruta, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
        using var zip = new ZipArchive(flujo, ZipArchiveMode.Read);
        var destinos = Relaciones(zip, "xl/_rels/workbook.bin.rels");
        var compartidas = zip.GetEntry("xl/sharedStrings.bin") is { } sst ? LeerCompartidas(sst) : [];
        var salida = new List<CabeceraHoja>();
        foreach (var (nombre, rel) in LeerHojas(zip.GetEntry("xl/workbook.bin") ?? throw new InvalidDataException("El .xlsb no tiene libro.")))
        {
            if (!destinos.TryGetValue(rel, out var destino)) continue;
            var entrada = zip.GetEntry("xl/" + destino.TrimStart('/').Replace("xl/", "", StringComparison.Ordinal));
            if (entrada is null) continue;
            salida.Add(new CabeceraHoja(nombre, LeerHoja(entrada, compartidas, filas, columnas)));
        }
        return salida;
    }

    /// <summary>Id de relación → destino («worksheets/sheet1.bin»).</summary>
    private static Dictionary<string, string> Relaciones(ZipArchive zip, string ruta)
    {
        var d = new Dictionary<string, string>(StringComparer.Ordinal);
        if (zip.GetEntry(ruta) is not { } e) return d;
        using var s = e.Open();
        foreach (var r in XDocument.Load(s).Root!.Elements())
        {
            if (r.Attribute("Id")?.Value is { } id && r.Attribute("Target")?.Value is { } t) d[id] = t;
        }
        return d;
    }

    /// <summary>Las hojas del libro, en orden: (nombre, id de relación).</summary>
    private static List<(string Nombre, string Rel)> LeerHojas(ZipArchiveEntry libro)
    {
        var hojas = new List<(string, string)>();
        using var s = new BufferedStream(libro.Open());
        while (Registro(s) is { } reg)
        {
            if (reg.Tipo != BundleSh) continue;
            var p = 8;   // hsState (4) + iTabID (4)
            var rel = Texto(reg.Datos, ref p, admiteNulo: true) ?? "";
            var nombre = Texto(reg.Datos, ref p) ?? "";
            hojas.Add((nombre, rel));
        }
        return hojas;
    }

    private static List<string> LeerCompartidas(ZipArchiveEntry e)
    {
        var lista = new List<string>();
        using var s = new BufferedStream(e.Open());
        while (Registro(s) is { } reg)
        {
            if (reg.Tipo != SstItem) continue;
            var p = 1;   // banderas (texto enriquecido / fonético)
            lista.Add(Texto(reg.Datos, ref p) ?? "");
        }
        return lista;
    }

    private static object?[][] LeerHoja(ZipArchiveEntry e, List<string> compartidas, int filas, int columnas)
    {
        var celdas = new object?[filas][];
        for (var i = 0; i < filas; i++) celdas[i] = new object?[columnas];
        using var s = new BufferedStream(e.Open());
        var fila = -1;
        while (Registro(s) is { } reg)
        {
            var d = reg.Datos;
            if (reg.Tipo == RowHdr)
            {
                fila = BitConverter.ToInt32(d, 0);
                if (fila >= filas) break;   // ya pasó la zona que interesa
                continue;
            }
            if (reg.Tipo is < CellBlank or > FmlaError || fila < 0 || d.Length < 8) continue;
            var col = BitConverter.ToInt32(d, 0);
            if (col < 0 || col >= columnas) continue;
            object? valor = reg.Tipo switch
            {
                CellRk => Rk(BitConverter.ToUInt32(d, 8)),
                CellReal or FmlaNum => BitConverter.ToDouble(d, 8),
                CellBool or FmlaBool => d[8] != 0,
                CellIsst => BitConverter.ToInt32(d, 8) is var i && i >= 0 && i < compartidas.Count ? compartidas[i] : null,
                CellSt or FmlaString => TextoEn(d, 8),
                _ => null,   // vacía o error
            };
            celdas[fila][col] = valor;
        }
        return celdas;
    }

    /// <summary>Un número RK: 30 bits de un double (o de un entero) con dos banderas (entero, ×100).</summary>
    private static double Rk(uint rk)
    {
        var x100 = (rk & 1) != 0;
        double v = (rk & 2) != 0
            ? (int)rk >> 2
            : BitConverter.Int64BitsToDouble((long)(rk & 0xFFFFFFFC) << 32);
        return x100 ? v / 100 : v;
    }

    private static string? TextoEn(byte[] d, int p) => Texto(d, ref p);

    /// <summary>Texto ancho: número de caracteres (4 bytes) y UTF-16; 0xFFFFFFFF es nulo.</summary>
    private static string? Texto(byte[] d, ref int p, bool admiteNulo = false)
    {
        if (p + 4 > d.Length) return null;
        var n = BitConverter.ToUInt32(d, p);
        p += 4;
        if (admiteNulo && n == 0xFFFFFFFF) return null;
        var bytes = (int)Math.Min(n * 2L, d.Length - p);
        var t = Encoding.Unicode.GetString(d, p, bytes);
        p += bytes;
        return t;
    }

    private sealed record Reg(int Tipo, byte[] Datos);

    /// <summary>El siguiente registro (tipo en 1–2 bytes y largo en 1–4, de 7 bits cada uno), o nulo al acabar.</summary>
    private static Reg? Registro(Stream s)
    {
        var b = s.ReadByte();
        if (b < 0) return null;
        var tipo = b & 0x7F;
        if ((b & 0x80) != 0)
        {
            var b2 = s.ReadByte();
            if (b2 < 0) return null;
            tipo |= (b2 & 0x7F) << 7;
        }
        var largo = 0;
        for (var i = 0; i < 4; i++)
        {
            var x = s.ReadByte();
            if (x < 0) return null;
            largo |= (x & 0x7F) << (7 * i);
            if ((x & 0x80) == 0) break;
        }
        var datos = new byte[largo];
        var leidos = 0;
        while (leidos < largo)
        {
            var n = s.Read(datos, leidos, largo - leidos);
            if (n <= 0) return null;
            leidos += n;
        }
        return new Reg(tipo, datos);
    }
}
