using System.Globalization;
using System.Text.RegularExpressions;

namespace CDM_Auditorias_Calidad.Servicios.Pesos;

/// <summary>Una carpeta de mes de incentivos («2026\09. SEPTIEMBRE»): los datos son del mes anterior (agosto).</summary>
/// <param name="Clave">«2026-09», el mes de la carpeta.</param>
public sealed record MesIncentivos(string Clave, string Ruta, int Año, int Mes)
{
    /// <summary>El mes de los datos: el anterior al de la carpeta (en «09. SEPTIEMBRE» se paga agosto).</summary>
    public DateOnly MesDatos => new DateOnly(Año, Mes, 1).AddMonths(-1);

    /// <summary>«09. SEPTIEMBRE».</summary>
    public string Carpeta => Path.GetFileName(Ruta);
}

/// <summary>El fichero elegido para un sector en un mes: la versión más reciente de las que hay.</summary>
public sealed record ArchivoElegido(string Sector, string Responsable, string Version, string Ruta, string RutaRelativa, DateTime Modificado, int Versiones);

/// <summary>
/// Dónde están los Excel de ranking y cuál leer de cada sector. Cada mes tiene <c>01. RANKING</c> con cuatro carpetas
/// (<c>01. PRECIERRE</c>, <c>02. V1</c>, <c>03. V2</c>, <c>04. VF</c>) y, dentro, una por responsable. De cada sector se
/// lee la versión más reciente: primero la carpeta (VF antes que V2…), luego la marca del nombre (VF, V3, V2, V1) y, si
/// empatan, la fecha de modificación.
/// </summary>
public static class ArchivosRanking
{
    public const string CarpetaRanking = "01. RANKING";

    private static readonly string[] Meses =
    [
        "enero", "febrero", "marzo", "abril", "mayo", "junio", "julio", "agosto", "septiembre", "setiembre", "octubre", "noviembre", "diciembre",
        "ene", "feb", "mar", "abr", "may", "jun", "jul", "ago", "sep", "sept", "oct", "nov", "dic",
    ];

    /// <summary>Las carpetas de mes que tienen ranking, de la más reciente a la más antigua.</summary>
    public static List<MesIncentivos> MesesDisponibles(string raiz)
    {
        var salida = new List<MesIncentivos>();
        if (!Directory.Exists(raiz)) return salida;
        foreach (var dirAño in Directory.EnumerateDirectories(raiz))
        {
            if (!int.TryParse(Path.GetFileName(dirAño), out var año)) continue;
            foreach (var dirMes in Directory.EnumerateDirectories(dirAño))
            {
                var m = Regex.Match(Path.GetFileName(dirMes), @"^(\d{1,2})\.");
                if (!m.Success || !Directory.Exists(Path.Combine(dirMes, CarpetaRanking))) continue;
                var mes = int.Parse(m.Groups[1].Value, CultureInfo.InvariantCulture);
                if (mes is < 1 or > 12) continue;
                salida.Add(new MesIncentivos($"{año:0000}-{mes:00}", dirMes, año, mes));
            }
        }
        return salida.OrderByDescending(m => m.Clave, StringComparer.Ordinal).ToList();
    }

    /// <summary>De cada sector del mes, el fichero más reciente.</summary>
    public static List<ArchivoElegido> Elegir(MesIncentivos mes)
    {
        var ranking = Path.Combine(mes.Ruta, CarpetaRanking);
        var candidatos = Directory.EnumerateFiles(ranking, "*.xls*", SearchOption.AllDirectories)
            .Where(EsRanking)
            .Select(r => (Ruta: r, Clave: ClaveSector(Path.GetFileName(r))))
            .Where(x => x.Clave.Length > 0)
            .GroupBy(x => x.Clave);
        var salida = new List<ArchivoElegido>();
        foreach (var g in candidatos)
        {
            var mejor = g.Select(x => x.Ruta).OrderByDescending(Rango).First();
            salida.Add(new ArchivoElegido(NombreSector(Path.GetFileName(mejor)), Responsable(ranking, mejor), Version(ranking, mejor),
                mejor, Path.GetRelativePath(ranking, mejor), File.GetLastWriteTime(mejor), g.Count()));
        }
        return salida.OrderBy(a => a.Sector, StringComparer.CurrentCulture).ToList();
    }

    /// <summary>
    /// Fuera los temporales de Excel (~$), las copias («… -.xlsb», «… Original», «… Estilo Orange», «… excepción»), las
    /// plantillas y los consolidados: de cada sector solo cuenta su fichero principal.
    /// </summary>
    public static bool EsRanking(string ruta)
    {
        var n = Path.GetFileName(ruta);
        var b = ExtractorPesos.SinTildes(n).ToLowerInvariant();
        return !n.StartsWith("~$", StringComparison.Ordinal) && !Regex.IsMatch(n, @" -\.xls", RegexOptions.IgnoreCase)
               && !Regex.IsMatch(b, @"plantilla|consolidado|thumbs|original|estilo|excepcion|copia");
    }

    /// <summary>
    /// La misma clave para todas las versiones de un sector: sin «Pre Cierre», «Ranking», versión, mes ni signos.
    /// «Pre Cierre CO Atencion Jazztel Agosto VF.xlsm» y «V1 CO Atención Jazztel AGO--.xlsm» → «co atencion jazztel».
    /// </summary>
    public static string ClaveSector(string nombre)
    {
        var s = ExtractorPesos.SinTildes(Path.GetFileNameWithoutExtension(nombre)).ToLowerInvariant();
        s = SinOrden(s);
        s = Regex.Replace(s, @"pr+e[\s\-]*cierre|ranking|\bde\b|\bv\d\b|\bvf\w*\b|\bpt\b|[-_$+]+", " ");
        return string.Join(' ', s.Split(' ', StringSplitOptions.RemoveEmptyEntries).Where(t => !Meses.Contains(t)));
    }

    /// <summary>«1.PreCierre …», «10. PreCierre …» → sin el número de orden que algunos ponen delante.</summary>
    private static string SinOrden(string s) => Regex.Replace(s, @"^\s*\d+\s*\.\s*", "");

    /// <summary>El nombre del sector para enseñar: el del fichero sin lo que sobra, con sus mayúsculas y tildes.</summary>
    public static string NombreSector(string nombre)
    {
        var s = SinOrden(Path.GetFileNameWithoutExtension(nombre));
        s = Regex.Replace(s, @"pr+e[\s\-]*cierre|ranking|\bde\b|\bv\d\b|\bvf\w*\b|\bpt\b|[-_$+]+", " ", RegexOptions.IgnoreCase);
        var palabras = s.Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Where(t => !Meses.Contains(ExtractorPesos.SinTildes(t).ToLowerInvariant()));
        var limpio = string.Join(' ', palabras);
        return limpio.Length == 0 ? nombre : char.ToUpper(limpio[0], CultureInfo.CurrentCulture) + limpio[1..];
    }

    /// <summary>(carpeta 1–4, marca del nombre: VF 9, V3 3, V2 2, V1 1, nada 0, fecha): cuanto mayor, más reciente.</summary>
    private static (int, int, DateTime) Rango(string ruta)
    {
        var carpeta = CarpetaVersion(ruta);
        var n = ExtractorPesos.SinTildes(Path.GetFileNameWithoutExtension(ruta)).ToLowerInvariant();
        var marca = Regex.IsMatch(n, @"\bvf") ? 9 : Regex.Match(n, @"\bv(\d)\b") is { Success: true } m ? int.Parse(m.Groups[1].Value, CultureInfo.InvariantCulture) : 0;
        return (carpeta, marca, File.GetLastWriteTime(ruta));
    }

    /// <summary>El número de la carpeta de versión (01. PRECIERRE → 1 … 04. VF → 4), la que cuelga de 01. RANKING.</summary>
    private static int CarpetaVersion(string ruta)
    {
        var dir = new DirectoryInfo(Path.GetDirectoryName(ruta)!);
        while (dir.Parent is { } padre && !padre.Name.Equals(CarpetaRanking, StringComparison.OrdinalIgnoreCase)) dir = padre;
        return Regex.Match(dir.Name, @"^(\d+)") is { Success: true } m ? int.Parse(m.Groups[1].Value, CultureInfo.InvariantCulture) : 0;
    }

    private static string Version(string ranking, string ruta)
    {
        var rel = Path.GetRelativePath(ranking, ruta);
        var primera = rel.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)[0];
        var nombre = Regex.Replace(primera, @"^\d+\.\s*", "");
        var n = ExtractorPesos.SinTildes(Path.GetFileNameWithoutExtension(ruta)).ToLowerInvariant();
        var marca = Regex.IsMatch(n, @"\bvf") ? "VF" : Regex.Match(n, @"\bv(\d)\b") is { Success: true } m ? "V" + m.Groups[1].Value : null;
        var carpeta = nombre.Equals("PRECIERRE", StringComparison.OrdinalIgnoreCase) ? "Precierre" : nombre.ToUpperInvariant();
        return marca is null || marca == carpeta ? carpeta : $"{carpeta} ({marca})";
    }

    /// <summary>«04. VF\05. Oscar\…» → «Oscar».</summary>
    private static string Responsable(string ranking, string ruta)
    {
        var partes = Path.GetRelativePath(ranking, ruta).Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        return partes.Length >= 3 ? Regex.Replace(partes[1], @"^\d+\.\s*", "") : "";
    }
}
