using CDM_Auditorias_Calidad.Models.Sectores;
using CDM_Auditorias_Calidad.Servicios.Sectores;
using Xunit;

namespace CDM_Auditorias_Calidad.Tests;

/// <summary>
/// «Rellamada y No solución por sector»: meses cerrados o en curso, mes por defecto, totales, filtros en cascada
/// y la tabla sector × mes, con unas filas hechas a mano (las cifras de agosto y septiembre de 2026 de tres sectores).
/// </summary>
public sealed class SectoresTests
{
    private static readonly DateOnly Hoy = new(2026, 10, 7);
    private const string R = "rellamada", N = "nosolucion";

    private static DatosSectores Datos() => new()
    {
        Filas = FuenteSectores.Juntar(
        [
            new(R, "2026-08", "CO Atención YGMM", "YGMM", "BigQuery", 102225, 22756),
            new(R, "2026-09", "CO Atención YGMM", "YGMM", "BigQuery", 106768, 23789),
            new(R, "2026-10", "CO Atención YGMM", "YGMM", "BigQuery", 20266, 3471),
            new(R, "2026-08", "CO Atención Jazztel", "JAZZTEL", "SQL Server", 68417, 14000),
            new(R, "2026-09", "CO Atención Jazztel", "JAZZTEL", "SQL Server", 72771, 15311),
            new(R, "2026-09", "CO Masivo Orange", "ORANGE", "SQL Server", 52653, 9971),
            new(N, "2026-09", "CO Atención YGMM", "YGMM", "BigQuery", 35194, 6186),
            new(N, "2026-10", "CO Atención YGMM", "YGMM", "BigQuery", 6959, 1326),
            // Una fila sin base no cuenta (y se quita al juntar).
            new(R, "2026-09", "Sin sector", "JAZZTEL", "SQL Server", 0, 0),
        ]),
        Generado = new DateTime(2026, 10, 7, 9, 0, 0),
    };

    [Fact]
    public void La_rellamada_cierra_un_mes_tres_dias_despues_y_la_no_solucion_al_acabar()
    {
        Assert.False(CalculadoraSectores.EnCurso("2026-09", IndicadorSectores.Rellamada, new DateOnly(2026, 10, 4)));
        Assert.True(CalculadoraSectores.EnCurso("2026-09", IndicadorSectores.Rellamada, new DateOnly(2026, 10, 3)));
        Assert.False(CalculadoraSectores.EnCurso("2026-09", IndicadorSectores.NoSolucion, new DateOnly(2026, 10, 1)));
        Assert.True(CalculadoraSectores.EnCurso("2026-10", IndicadorSectores.NoSolucion, Hoy));
    }

    [Fact]
    public void Por_defecto_el_mes_es_el_ultimo_cerrado_y_se_compara_con_el_anterior()
    {
        var r = CalculadoraSectores.Resolver(Datos(), new PeticionSectores(), IndicadorSectores.Rellamada, Hoy);

        Assert.Equal(["2026-08", "2026-09", "2026-10"], r.Meses.Select(m => m.Clave));
        Assert.True(r.Meses[^1].EnCurso);
        Assert.Equal("2026-09", r.Mes.Clave);
        Assert.True(r.MesPorDefecto);
        Assert.Equal("2026-08", r.MesAnterior!.Clave);

        var sep = r.Total("2026-09");
        Assert.Equal(106768 + 72771 + 52653, sep.Base);
        Assert.Equal(23789 + 15311 + 9971, sep.Casos);
        Assert.Equal((double)sep.Casos / sep.Base - (22756.0 + 14000) / (102225 + 68417), r.Variacion!.Value, 10);
        Assert.DoesNotContain(r.Tabla, f => f.Sector == "Sin sector");
        Assert.Equal("CO Atención YGMM", r.PorSectorDelMes[0].Sector);
    }

    [Fact]
    public void Los_filtros_van_en_cascada_y_lo_que_no_existe_se_ignora()
    {
        var p = new PeticionSectores { Marca = ["JAZZTEL", "MOVISTAR"], Sector = ["Ya no existe"] };
        var r = CalculadoraSectores.Resolver(Datos(), p, IndicadorSectores.Rellamada, Hoy);

        Assert.Equal(["JAZZTEL"], r.Peticion.Marca);
        Assert.Empty(r.Peticion.Sector);
        Assert.Equal(["CO Atención Jazztel"], r.Tabla.Select(f => f.Sector));
        // El desplegable de sector solo ofrece los de la marca marcada; el de marca, las tres.
        Assert.Equal(["CO Atención Jazztel"], r.Desplegables.Single(g => g.Campo == "sector").Opciones.Select(o => o.Valor));
        Assert.Equal(3, r.Desplegables.Single(g => g.Campo == "marca").Opciones.Count);
        Assert.Equal(15311.0 / 72771, r.Total("2026-09").Pct!.Value, 10);
    }

    [Fact]
    public void Un_mes_elegido_en_la_url_se_respeta_y_viaja_en_los_enlaces()
    {
        var r = CalculadoraSectores.Resolver(Datos(), new PeticionSectores { Mes = "2026-10" }, IndicadorSectores.NoSolucion, Hoy);

        Assert.Equal("2026-10", r.Mes.Clave);
        Assert.True(r.Mes.EnCurso);
        Assert.False(r.MesPorDefecto);
        Assert.Contains(new KeyValuePair<string, string>("mes", "2026-10"), r.Parametros());

        var pagina = new PaginaSectores { Indicador = IndicadorSectores.NoSolucion, R = r };
        Assert.Equal("/sectores?mes=2026-10", pagina.UrlIndicador(IndicadorSectores.Rellamada));
        Assert.Equal("/sectores/csv", pagina.UrlCsv);
    }

    [Theory]
    [InlineData(0.0058, "+0,58 %")]
    [InlineData(-0.0115, "−1,15 %")]
    [InlineData(0, "0,00 %")]
    public void Las_diferencias_se_escriben_como_una_resta(double fraccion, string esperado)
        => Assert.Equal(esperado, PaginaSectores.Diferencia(fraccion));

    [Fact]
    public void El_color_sigue_a_la_marca_filtrada()
    {
        PaginaSectores Con(params string[] marcas) => new()
        {
            Indicador = IndicadorSectores.Rellamada,
            R = CalculadoraSectores.Resolver(Datos(), new PeticionSectores { Marca = marcas.ToList() }, IndicadorSectores.Rellamada, Hoy),
        };
        Assert.Equal("ygmm", Con("YGMM").MarcaPagina);
        Assert.Equal("jazztel", Con("JAZZTEL").MarcaPagina);
        Assert.Equal("orange", Con("YGMM", "JAZZTEL").MarcaPagina);
    }
}
