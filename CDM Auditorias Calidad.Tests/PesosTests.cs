using CDM_Auditorias_Calidad.Models.Pesos;
using CDM_Auditorias_Calidad.Servicios.Configuracion;
using CDM_Auditorias_Calidad.Servicios.Pesos;
using Xunit;

namespace CDM_Auditorias_Calidad.Tests;

/// <summary>
/// «Pesos y metas por sector»: el bloque de objetivos se encuentra por su cabecera de metas (no por el rótulo), los
/// bloques apilados no se mezclan, la clave del sector junta todas las versiones de un fichero y la validación.
/// </summary>
public sealed class PesosTests
{
    /// <summary>Una hoja de 20 × 12 con las celdas dadas (fila y columna desde 1).</summary>
    private static CabeceraHoja Hoja(string nombre, params (int Fila, int Col, object Valor)[] celdas)
    {
        var g = Enumerable.Range(0, 20).Select(_ => new object?[12]).ToArray();
        foreach (var (f, c, v) in celdas) g[f - 1][c - 1] = v;
        return new CabeceraHoja(nombre, g);
    }

    [Fact]
    public void Encuentra_el_bloque_aunque_no_ponga_peso_y_con_cifras_en_texto()
    {
        // Como Atención YGMM: «Objetivos CO Atención», la columna del peso sin rótulo y las metas 0 | 1 | 1,5 (una en texto).
        var h = Hoja("CO Atención YGMM",
            (3, 2, "Objetivos CO Atención"), (3, 4, 0d), (3, 5, "1"), (3, 6, 1.5),
            (4, 2, "Renove"), (4, 3, 0.3), (4, 4, 0.17), (4, 5, 0.27), (4, 6, 0.4),
            (5, 2, "%RLL 72H"), (5, 3, 0.3), (5, 4, 0.25), (5, 5, 0.22), (5, 6, 0.17),
            (6, 2, "%No Sol"), (6, 3, "40%"), (6, 4, 0.225), (6, 5, 0.175), (6, 6, 0.14));
        var b = Assert.Single(ExtractorPesos.Extraer(h));
        Assert.Equal("Objetivos CO Atención", b.Titulo);
        Assert.Equal([0, 1, 1.5], b.Cabecera);
        Assert.Equal(["Renove", "%RLL 72H", "%No Sol"], b.Filas.Select(f => f.Kpi));
        Assert.Equal(0.4, b.Filas[2].Peso!.Value, 10);
        Assert.Equal(1.0, b.SumaPesos, 10);
        Assert.Equal("Agente", b.Nivel);
    }

    [Fact]
    public void Dos_bloques_apilados_no_se_mezclan_y_la_hoja_suma_100()
    {
        // Como Técnico Orange: un bloque por skill, uno debajo del otro, que se reparten el 100 %.
        var h = Hoja("Ranking AG",
            (4, 2, "CO Tecnico Orange 407"), (4, 3, " Peso"), (4, 4, "0"), (4, 5, "1"), (4, 6, 1.5),
            (5, 2, "TMO"), (5, 3, 0.15), (5, 4, 700d), (5, 5, 630d), (5, 6, 530d),
            (6, 2, "Rell 72 Hrs"), (6, 3, 0.42), (6, 4, 0.24), (6, 5, 0.18), (6, 6, 0.14),
            (7, 2, "CO Tecnico Orange 441"), (7, 3, " Peso"), (7, 4, "0"), (7, 5, "1"), (7, 6, 1.5),
            (8, 2, "TMO"), (8, 3, 0.43), (8, 4, 640d), (8, 5, 550d), (8, 6, 500d));
        var bloques = ExtractorPesos.Extraer(h);
        Assert.Equal(2, bloques.Count);
        Assert.Equal(["TMO", "Rell 72 Hrs"], bloques[0].Filas.Select(f => f.Kpi));
        Assert.Equal(["TMO"], bloques[1].Filas.Select(f => f.Kpi));
        Assert.Null(PaginaPesos.ValidarSuma(bloques, "Técnico Orange"));
    }

    [Fact]
    public void Lee_el_bloque_con_columna_vacia_y_lo_parte_por_grupos()
    {
        // Como CO WhatsApp Técnico: «KPIs | (vacía) | % | 0 | 1 | 1,5», el grupo a la izquierda («WhatsApp», «Tecnico»).
        var h = Hoja("Ranking Agente",
            (4, 3, "KPIs"), (4, 5, "%"), (4, 6, "0"), (4, 7, "1"), (4, 8, 1.5),
            (5, 2, "WhatsApp"), (5, 3, "Productividad"), (5, 5, 0.2), (5, 6, 3d), (5, 7, 4.5), (5, 8, 8d),
            (6, 3, "Re-Chat"), (6, 5, 0.5), (6, 6, 18d), (6, 7, 12d), (6, 8, 9d),
            (7, 3, "Recontacto"), (7, 5, 0.3), (7, 6, 35d), (7, 7, 25d), (7, 8, 16d),
            (8, 2, "Tecnico"), (8, 3, "TMO"), (8, 5, 0.6), (8, 6, 730d), (8, 7, 620d), (8, 8, 580d),
            (9, 3, "Transfer"), (9, 5, 0.4), (9, 6, 7d), (9, 7, 5d), (9, 8, 2d));
        var bloques = ExtractorPesos.Extraer(h);
        Assert.Equal(2, bloques.Count);
        Assert.Equal("WhatsApp", bloques[0].Titulo);
        Assert.Equal(["Productividad", "Re-Chat", "Recontacto"], bloques[0].Filas.Select(f => f.Kpi));
        Assert.Equal([35d, 25d, 16d], bloques[0].Filas[2].Metas.Select(m => m!.Value));
        Assert.Equal("Tecnico", bloques[1].Titulo);
        Assert.Equal(1.0, bloques[1].SumaPesos, 10);
    }

    [Fact]
    public void Lee_las_cuatro_metas_de_atencion_jazztel()
    {
        var h = Hoja("Ranking AG",
            (4, 2, "Objetivos Atención Jazztel"), (4, 3, "Ponderacion"), (4, 4, 0d), (4, 5, 0.6), (4, 6, 1d), (4, 7, 1.5),
            (5, 2, "% Rellamada 72H"), (5, 3, 1d), (5, 4, 0.208), (5, 5, 0.196), (5, 6, 0.188), (5, 7, 0.155));
        var b = Assert.Single(ExtractorPesos.Extraer(h));
        Assert.Equal([0, 0.6, 1, 1.5], b.Cabecera);
        Assert.Equal(0.196, b.Filas[0].Metas[1]!.Value, 10);
    }

    [Fact]
    public void Una_fila_de_datos_no_se_confunde_con_una_cabecera()
    {
        // 0,2 | 0,8 | 1,5 no empieza en 0; 0 | 2 | 4 no incluye el 1.
        Assert.False(ExtractorPesos.EsCabeceraDeMetas([0.2, 0.8, 1.5]));
        Assert.False(ExtractorPesos.EsCabeceraDeMetas([0, 2, 4]));
        Assert.False(ExtractorPesos.EsCabeceraDeMetas([0, 1]));
        Assert.True(ExtractorPesos.EsCabeceraDeMetas([0, 0.6, 1, 1.5]));
    }

    [Fact]
    public void Cada_sector_se_queda_con_su_hoja_principal_de_agentes()
    {
        BloquePesos B(string hoja) => new(hoja, ExtractorPesos.Nivel(hoja), null, [0, 1, 1.5], [new("TMO", 1, [700, 650, 600])]);
        // Por nombre, aunque no sea la primera y con espacios de más.
        Assert.Equal("Ranking AG ", ExtractorPesos.HojaPrincipal([B("Ranking AG Mes 1"), B("Ranking AG "), B("Ranking TL")]));
        Assert.Equal("Ranking AG Universal", ExtractorPesos.HojaPrincipal([B("Ranking AG Front"), B("Ranking AG"), B("Ranking AG Universal")]));
        Assert.Equal("Ranking AGENTE", ExtractorPesos.HojaPrincipal([B("Ranking AGENTE - Traspasos"), B("Ranking AGENTE")]));
        // Sin ninguno de esos nombres, la primera de agentes del libro (Atención YGMM, Técnico MasMovil).
        Assert.Equal("CO Atención YGMM", ExtractorPesos.HojaPrincipal([B("Ranking TL"), B("CO Atención YGMM"), B("CO Atención YGMM Mes 2")]));
        Assert.Equal("Senior", ExtractorPesos.HojaPrincipal([B("Senior"), B("Ranking Agentes TLT")]));
        Assert.Null(ExtractorPesos.HojaPrincipal([B("Ranking TL"), B("Ranking SP")]));
    }

    [Fact]
    public void Una_hoja_extra_sale_como_su_propio_sector()
    {
        BloquePesos B(string hoja, string kpi) => new(hoja, ExtractorPesos.Nivel(hoja), null, [0, 1, 1.5], [new(kpi, 1, [0.5, 1, 1.8])]);
        var archivo = new ArchivoElegido("Gestion pedidos", "Debinson", "V1", @"X:. RANKING. V1. Debinson\Ranking de Gestion de pedidos Agosto V1.xlsb",
            @"02. V1. Debinson\Ranking de Gestion de pedidos Agosto V1.xlsb", DateTime.Today, 2);
        var analisis = new AnalisisPesos
        {
            Mes = "2026-09",
            Archivos = [new(archivo, "ok", null, [B("Ranking AG Gestion Incidencias", "Casos/Hora"), B("Ranking AG Seguro Movil", "Casos/Hora"), B("Ranking AG M", "Otro")])],
        };
        var extras = new[] { new HojaExtraPesos { Fichero = "Ranking de Gestion de pedidos.xlsb", Hoja = "ranking ag  seguro movil", Sector = "Bo Seguro Móvil" } };
        var (filas, _, desplegables, _) = PaginaPesos.Construir(analisis, new PeticionPesos(), null, extras);
        Assert.Equal(["Gestion pedidos", "Bo Seguro Móvil"], filas.Select(f => f.Sector));
        Assert.DoesNotContain(filas, f => f.Kpi == "Otro");
        Assert.Equal(2, desplegables.Single(d => d.Campo == "sector").Opciones.Count);
    }

    [Theory]
    [InlineData("Ranking AG", "Agente")]
    [InlineData("Ranking Agentes TLT", "Agente")]
    [InlineData("Ranking TL ", "Team leader")]
    [InlineData("Ranking Team Leader", "Team leader")]
    [InlineData("Ranking SP Carlos", "Supervisor")]
    [InlineData("Ranking JS", "Jefe de servicio")]
    public void El_nivel_sale_del_nombre_de_la_hoja(string hoja, string nivel) => Assert.Equal(nivel, ExtractorPesos.Nivel(hoja));

    [Theory]
    [InlineData("Pre Cierre CO Atencion Jazztel Agosto VF.xlsm", "co atencion jazztel")]
    [InlineData("V1 CO Atención Jazztel AGO--.xlsm", "co atencion jazztel")]
    [InlineData("Pre Cierre CO Fide Out Agosto V3.xlsx", "co fide out")]
    [InlineData("1.PreCierre  Ranking  CO Whatsapp YGMM Técnico Septiembre.xlsx", "co whatsapp ygmm tecnico")]
    [InlineData("V3 - Ranking CO WhatsApp YGMM Agosto.xlsm", "co whatsapp ygmm")]
    public void La_clave_junta_todas_las_versiones_de_un_sector(string fichero, string clave)
        => Assert.Equal(clave, ArchivosRanking.ClaveSector(fichero));

    [Theory]
    [InlineData("Ranking Masivo Orange Agosto V1 Original.xlsm", false)]
    [InlineData("Pre Cierre Ranking CO BO Comercial Agosto -.xlsb", false)]
    [InlineData("~$Pre Cierre CO Atencion Jazztel Agosto.xlsm", false)]
    [InlineData("Ranking Masivo Orange Agosto V1.xlsm", true)]
    public void Fuera_las_copias_y_los_temporales(string fichero, bool entra) => Assert.Equal(entra, ArchivosRanking.EsRanking(fichero));

    [Fact]
    public void La_validacion_distingue_lo_que_hay_que_revisar_de_las_notas()
    {
        var b = new BloquePesos("Ranking TL", "Team leader", "Cmto Equipo", [0, 1, 1.5],
        [
            new("Ponderación Equipo AG", 0.6, [null, null, null]),      // se calcula aparte: nota
            new("Ausentismo", 0.1, [5.1, null, 3.8]),                     // le falta una meta: revisar
            new("Rotación", 0.1, [6.3, 4.5, 5.5]),                        // no van en un solo sentido: revisar
            new("Llave", null, [0.8, null, null]),                        // sin peso: nota
        ]);
        var v = PaginaPesos.Validar(b, "X");
        Assert.Equal(2, v.Count(c => c.Correcto == false));
        Assert.Equal(2, v.Count(c => c.Correcto is null));
        Assert.Contains(v, c => c.Correcto == false && c.Texto.Contains("«Ausentismo»") && c.Texto.Contains("100"));
        var suma = PaginaPesos.ValidarSuma([b], "X");
        Assert.NotNull(suma);
        Assert.Contains("80", suma!.Texto);
    }
}
