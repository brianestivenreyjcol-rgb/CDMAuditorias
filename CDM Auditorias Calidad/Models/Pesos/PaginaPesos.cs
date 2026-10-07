using System.Globalization;
using CDM_Auditorias_Calidad.Infraestructura;
using CDM_Auditorias_Calidad.Servicios.Configuracion;
using CDM_Auditorias_Calidad.Servicios.Pesos;
using Microsoft.AspNetCore.WebUtilities;

namespace CDM_Auditorias_Calidad.Models.Pesos;

/// <summary>Los parámetros de la URL: el mes («2026-09») y los filtros (repetibles).</summary>
public sealed class PeticionPesos
{
    public string? Mes { get; set; }
    public List<string> Sector { get; set; } = new();
    public List<string> Responsable { get; set; } = new();
    /// <summary>«revisar»: solo lo que tiene avisos.</summary>
    public string? Ver { get; set; }
}

/// <summary>Una fila de la tabla: un KPI de un bloque, con sus metas en las cuatro columnas fijas (0 %, 60 %, 100 %, 150 %).</summary>
/// <param name="Ruta">La ruta del Excel como la abre el usuario (<c>Y:\…</c>), para copiarla y validar.</param>
public sealed record FilaTablaPesos(string Sector, string Responsable, string Hoja, string Kpi, double? Peso,
    double? Meta0, double? Meta60, double? Meta100, double? Meta150, bool EnPorcentaje, bool ConAvisos, string Ruta);

/// <summary>Una comprobación: <c>Correcto</c> verde, <c>false</c> «Revisar», nulo «Nota».</summary>
public sealed record ComprobacionPesos(bool? Correcto, string Sector, string Texto);

/// <summary>
/// «Pesos y metas por sector»: lo analizado de un mes, filtrado, con sus comprobaciones. Solo la hoja principal de agentes de
/// cada sector (pedido del usuario el 07-10-2026, <see cref="ExtractorPesos.HojaPrincipal"/>): las de TL, supervisor, jefe de
/// servicio y las variantes («Mes 1», «TLT»…) se leen pero no se enseñan.
/// </summary>
public sealed class PaginaPesos
{
    public const string Ruta = "/pesos";
    public const double Tolerancia = 0.005;

    public required IReadOnlyList<MesIncentivos> Meses { get; init; }
    public MesIncentivos? Mes { get; init; }
    public AnalisisPesos? Analisis { get; init; }
    public required PeticionPesos Peticion { get; init; }
    public bool Analizando { get; init; }
    public string? MesEnCurso { get; init; }
    public string? Progreso { get; init; }
    public string? Error { get; init; }
    public string? Aviso { get; init; }
    public string Raiz { get; init; } = "";

    /// <summary>La ruta de un fichero como la abre el usuario (<c>Y:\…</c>).</summary>
    public Func<string, string> RutaVisible { get; init; } = r => r;

    /// <summary>Las hojas extra que son otro sector (appsettings, <c>Pesos:HojasExtra</c>).</summary>
    public IReadOnlyList<HojaExtraPesos> Extras { get; init; } = [];

    /// <summary>Las hojas que se leen de un fichero: la principal y las extra que tenga.</summary>
    public string HojasLeidas(ArchivoAnalizado a)
    {
        var clave = ArchivosRanking.ClaveSector(Path.GetFileName(a.Archivo.Ruta));
        var hojas = new[] { ExtractorPesos.HojaPrincipal(a.Bloques) }
            .Concat(Extras.Where(e => ArchivosRanking.ClaveSector(e.Fichero) == clave && a.Bloques.Any(b => ExtractorPesos.MismaHoja(b.Hoja, e.Hoja))).Select(e => e.Hoja))
            .Where(h => h is not null).ToList();
        return hojas.Count == 0 ? "—" : string.Join(" + ", hojas);
    }

    public IReadOnlyList<GrupoFiltro> Desplegables { get; init; } = [];
    public IReadOnlyList<FilaTablaPesos> Filas { get; init; } = [];
    public IReadOnlyList<ComprobacionPesos> Comprobaciones { get; init; } = [];
    public IReadOnlyList<ArchivoAnalizado> Archivos { get; init; } = [];

    public bool AnalizandoEsteMes => Analizando && Mes is not null && MesEnCurso == Mes.Clave;

    /// <summary>El mes como está en la carpeta (pedido del usuario): «09. SEPTIEMBRE 2026»; corto, «09. SEPTIEMBRE».</summary>
    public static string TextoMes(MesIncentivos m) => $"{m.Carpeta} {m.Año}";
    public static string TextoMesCorto(MesIncentivos m) => m.Carpeta;

    /// <summary>El mes de los datos (el anterior al de la carpeta): «agosto de 2026».</summary>
    public static string TextoDatos(MesIncentivos m) => m.MesDatos.ToString("MMMM 'de' yyyy", Formato.Es);

    public IEnumerable<KeyValuePair<string, string>> Parametros(bool conMes = true)
    {
        if (conMes && Mes is not null && Mes != Meses.FirstOrDefault()) yield return new("mes", Mes.Clave);
        foreach (var s in Peticion.Sector) yield return new("sector", s);
        foreach (var r in Peticion.Responsable) yield return new("responsable", r);
        if (Peticion.Ver == "revisar") yield return new("ver", "revisar");
    }

    public string UrlMes(MesIncentivos m) => Con(Ruta, Parametros(conMes: false).Append(new("mes", m.Clave)));
    public string UrlVer(string? ver) => Con(Ruta, Parametros().Where(p => p.Key != "ver").Concat(ver is null ? [] : [new KeyValuePair<string, string>("ver", ver)]));
    public string UrlLimpiar => Con(Ruta, Mes is not null && Mes != Meses.FirstOrDefault() ? [new("mes", Mes.Clave)] : []);
    public string UrlCsv => Con(Ruta + "/csv", Parametros());

    private static string Con(string ruta, IEnumerable<KeyValuePair<string, string>> p)
        => QueryHelpers.AddQueryString(ruta, p.Select(x => new KeyValuePair<string, string?>(x.Key, x.Value)));

    // ---------------------------------------------------------------------------------------------------------------

    /// <summary>Una meta o un umbral como se lee: en % si todas las metas de la fila caben en ±150 %; si no, la cifra.</summary>
    public static string Meta(double? v, bool enPorcentaje)
        => v is not { } x ? "—" : enPorcentaje ? Formato.Porcentaje(x) : x.ToString("#,##0.##", Formato.Es);

    /// <summary>Las filas de la tabla, las comprobaciones y los desplegables de un análisis con estos filtros.</summary>
    public static (List<FilaTablaPesos> Filas, List<ComprobacionPesos> Comprobaciones, List<GrupoFiltro> Desplegables, List<ArchivoAnalizado> Archivos)
        Construir(AnalisisPesos analisis, PeticionPesos p, Func<string, string>? rutaVisible = null, IReadOnlyList<HojaExtraPesos>? extras = null)
    {
        rutaVisible ??= r => r;
        extras ??= [];
        bool Pasa(string valor, List<string> marcados) => marcados.Count == 0 || marcados.Contains(valor);
        var todas = new List<(FilaTablaPesos Fila, ArchivoAnalizado Archivo)>();
        var comprobaciones = new List<ComprobacionPesos>();
        // Los sectores que da cada fichero (el suyo y, si los hay, los de sus hojas extra).
        var sectoresDe = analisis.Archivos.ToDictionary(a => a, a => new List<string>());

        foreach (var a in analisis.Archivos)
        {
            var sector = a.Archivo.Sector;
            switch (a.Estado)
            {
                case "xlsb":
                case "error":
                case "sin-bloques":
                    comprobaciones.Add(new(false, sector, (a.Error ?? "No se pudo leer.") + " (" + a.Archivo.RutaRelativa + ")"));
                    continue;
            }
            // Una sola hoja por sector: la principal de agentes («Ranking AG Universal», «Ranking AG», «Ranking AGENTE»…) y, si está en
            // OpcionesPesos.HojasExtra, alguna más que en ese Excel es otro sector (Bo Seguro Móvil dentro de Gestión pedidos).
            var principal = ExtractorPesos.HojaPrincipal(a.Bloques);
            var unidades = new List<(string Sector, List<BloquePesos> Bloques)> { (sector, a.Bloques.Where(b => b.Hoja == principal).ToList()) };
            var clave = ArchivosRanking.ClaveSector(Path.GetFileName(a.Archivo.Ruta));
            foreach (var e in extras.Where(e => ArchivosRanking.ClaveSector(e.Fichero) == clave))
            {
                var bloquesExtra = a.Bloques.Where(b => ExtractorPesos.MismaHoja(b.Hoja, e.Hoja)).ToList();
                if (bloquesExtra.Count > 0) unidades.Add((e.Sector, bloquesExtra));
            }
            if (unidades[0].Bloques.Count == 0 && unidades.Count == 1)
            {
                comprobaciones.Add(new(null, sector, "Solo tiene bloques de team leader, supervisor o jefe de servicio: no es un ranking de agentes."));
                continue;
            }
            foreach (var (sectorUnidad, bloquesAgente) in unidades.Where(u => u.Bloques.Count > 0))
            {
                sectoresDe[a].Add(sectorUnidad);
                Unidad(a, sectorUnidad, bloquesAgente);
            }
        }

        void Unidad(ArchivoAnalizado a, string sector, List<BloquePesos> bloquesAgente)
        {
            var avisosArchivo = 0;
            // La suma de pesos se mira por hoja: algunas reparten el 100 % entre varios bloques (uno por skill).
            var hojasMal = new HashSet<string>();
            foreach (var hoja in bloquesAgente.GroupBy(b => b.Hoja))
            {
                if (ValidarSuma(hoja.ToList(), sector) is { } aviso)
                {
                    comprobaciones.Add(aviso);
                    hojasMal.Add(hoja.Key);
                    avisosArchivo++;
                }
            }
            foreach (var b in bloquesAgente)
            {
                var avisosBloque = Validar(b, sector);
                comprobaciones.AddRange(avisosBloque);
                avisosArchivo += avisosBloque.Count(c => c.Correcto == false);
                foreach (var f in b.Filas)
                {
                    double? MetaEn(double nivel)
                    {
                        var i = b.Cabecera.FindIndex(c => Math.Abs(c - nivel) < 1e-9);
                        return i >= 0 && i < f.Metas.Count ? f.Metas[i] : null;
                    }
                    var metas = f.Metas.Where(m => m is not null).Select(m => m!.Value).ToList();
                    var enPorcentaje = metas.Count > 0 && metas.All(m => Math.Abs(m) <= 1.5);
                    var conAvisos = hojasMal.Contains(b.Hoja) || avisosBloque.Any(c => c.Correcto == false && c.Texto.Contains($"«{f.Kpi}»"));
                    todas.Add((new FilaTablaPesos(sector, a.Archivo.Responsable, b.Hoja, f.Kpi, f.Peso,
                        MetaEn(0), MetaEn(0.6), MetaEn(1), MetaEn(1.5), enPorcentaje, conAvisos, rutaVisible(a.Archivo.Ruta)), a));
                }
            }
            if (avisosArchivo == 0) comprobaciones.Add(new(true, sector, $"{bloquesAgente.Count} {(bloquesAgente.Count == 1 ? "bloque" : "bloques")} de agentes sin nada que revisar ({a.Archivo.Version})."));
        }

        var filtradas = todas.Where(x => Pasa(x.Fila.Sector, p.Sector) && Pasa(x.Fila.Responsable, p.Responsable)
                                         && (p.Ver != "revisar" || x.Fila.ConAvisos)).Select(x => x.Fila).ToList();
        var sectoresVisibles = analisis.Archivos.Where(a => Pasa(a.Archivo.Responsable, p.Responsable))
            .SelectMany(a => sectoresDe[a].Append(a.Archivo.Sector)).Where(x => Pasa(x, p.Sector)).ToHashSet();

        GrupoFiltro Grupo(string campo, string titulo, IEnumerable<string> valores, List<string> marcados, Func<FilaTablaPesos, bool> otros, Func<FilaTablaPesos, string> de)
            => new(campo, titulo, valores.Distinct().Concat(marcados).Distinct()
                .OrderBy(v => v, StringComparer.CurrentCulture)
                .Select(v => new OpcionFiltro(v, v, todas.Count(x => otros(x.Fila) && de(x.Fila) == v), marcados.Contains(v))).ToList());

        var desplegables = new List<GrupoFiltro>
        {
            Grupo("sector", "Sector", todas.Where(x => Pasa(x.Fila.Responsable, p.Responsable)).Select(x => x.Fila.Sector), p.Sector,
                f => Pasa(f.Responsable, p.Responsable), f => f.Sector),
            Grupo("responsable", "Responsable", todas.Select(x => x.Fila.Responsable).Where(r => r.Length > 0), p.Responsable,
                f => Pasa(f.Sector, p.Sector), f => f.Responsable),
        };

        return (filtradas,
            comprobaciones.Where(c => sectoresVisibles.Contains(c.Sector)).OrderBy(c => c.Correcto switch { false => 0, null => 1, _ => 2 })
                .ThenBy(c => c.Sector, StringComparer.CurrentCulture).ToList(),
            desplegables,
            analisis.Archivos.Where(a => sectoresDe[a].Append(a.Archivo.Sector).Any(sectoresVisibles.Contains)).ToList());
    }

    /// <summary>
    /// Lo que se revisa de cada bloque: que los pesos sumen 100 %, que cada KPI con peso tenga todas sus metas y que las
    /// metas vayan en un solo sentido (subiendo o bajando). Un KPI sin peso es un requisito o una llave: solo se anota.
    /// </summary>
    /// <summary>
    /// La suma de pesos de una hoja: vale si cada bloque suma 100 % (Atención Jazztel: Atención y Blending, uno al lado del
    /// otro) o si todos juntos suman 100 % (Técnico Orange: un bloque por skill, 57 % + 43 %). Si no, un aviso.
    /// </summary>
    public static ComprobacionPesos? ValidarSuma(List<BloquePesos> bloques, string sector)
    {
        // Un bloque con todos los pesos a 0 no se aplica este mes: no cuenta (Validar lo anota).
        bloques = bloques.Where(b => b.SumaPesos > Tolerancia).ToList();
        if (bloques.Count == 0) return null;
        if (bloques.All(b => Math.Abs(b.SumaPesos - 1) <= Tolerancia)) return null;
        var total = bloques.Sum(b => b.SumaPesos);
        if (Math.Abs(total - 1) <= Tolerancia) return null;
        var detalle = bloques.Count == 1 ? Formato.Porcentaje(total) : string.Join(" + ", bloques.Select(b => Formato.Porcentaje(b.SumaPesos)));
        return new(false, sector, $"Los pesos de «{bloques[0].Hoja}» suman {detalle}, no 100 %.");
    }

    public static List<ComprobacionPesos> Validar(BloquePesos b, string sector)
    {
        var salida = new List<ComprobacionPesos>();
        var donde = $"«{b.Hoja}»";
        if (b.SumaPesos <= Tolerancia)
        {
            salida.Add(new(null, sector, $"Un bloque de {donde}{(b.Titulo is null ? "" : $" («{b.Titulo}»)")} tiene todos los pesos a 0: este mes no se aplica."));
            return salida;
        }
        foreach (var f in b.Filas)
        {
            if (f.Peso is null)
            {
                salida.Add(new(null, sector, $"«{f.Kpi}» en {donde} no tiene peso: es un requisito o una llave, no puntúa."));
                continue;
            }
            if (f.Peso == 0) continue;
            // Solo las metas que se enseñan (0, 100 y 150 %): la del 60 % no sale en la vista (pedido del usuario el 07-10-2026).
            var vistas = f.Metas.Select((m, i) => (Meta: m, Nivel: i < b.Cabecera.Count ? b.Cabecera[i] : -1))
                .Where(x => Math.Abs(x.Nivel - 0.6) > 1e-9).ToList();
            var faltan = vistas.Where(x => x.Meta is null).Select(x => Formato.PorcentajeEntero(x.Nivel * 100)).ToList();
            if (faltan.Count == vistas.Count)
            {
                // «Ponderación Equipo AG», «Promedio 3M»: pesa, pero se calcula aparte (el cumplimiento del equipo…), sin metas.
                salida.Add(new(null, sector, $"«{f.Kpi}» en {donde} pesa {Formato.Porcentaje(f.Peso)} y no tiene metas en el bloque: se calcula aparte (p. ej. el cumplimiento del equipo)."));
                continue;
            }
            if (faltan.Count > 0)
            {
                salida.Add(new(false, sector, $"«{f.Kpi}» en {donde} pesa {Formato.Porcentaje(f.Peso)} y le falta la meta del {string.Join(", ", faltan)}."));
                continue;
            }
            var m = vistas.Select(x => x.Meta!.Value).ToList();
            var sube = m.Zip(m.Skip(1)).All(z => z.Second >= z.First);
            var baja = m.Zip(m.Skip(1)).All(z => z.Second <= z.First);
            if (!sube && !baja)
            {
                salida.Add(new(false, sector, $"«{f.Kpi}» en {donde}: las metas no van en un solo sentido ({string.Join(" · ", m.Select(x => x.ToString("0.###", CultureInfo.GetCultureInfo("es-ES"))))})."));
            }
        }
        return salida;
    }
}
