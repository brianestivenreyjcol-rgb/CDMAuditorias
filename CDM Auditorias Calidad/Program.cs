using System.Globalization;
using CDM_Auditorias_Calidad.Servicios.Configuracion;
using CDM_Auditorias_Calidad.Servicios.Datos;
using CDM_Auditorias_Calidad.Servicios.Gaia;
using CDM_Auditorias_Calidad.Servicios.NoSolucion;
using CDM_Auditorias_Calidad.Servicios.Pesos;
using CDM_Auditorias_Calidad.Servicios.Sectores;

// Números y fechas como en el Power BI (es-ES). Las coordenadas de los gráficos se
// escriben siempre con cultura invariable (Formato.Coord).
CultureInfo.DefaultThreadCurrentCulture = CultureInfo.GetCultureInfo("es-ES");
CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.GetCultureInfo("es-ES");

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();
builder.Services.Configure<OpcionesAuditorias>(builder.Configuration.GetSection(OpcionesAuditorias.Seccion));
builder.Services.AddSingleton<RepositorioAuditorias>();
builder.Services.AddSingleton<AlmacenAuditorias>();
builder.Services.AddHostedService<RecargaPeriodica>();

// CDM No solución (traído de ranking-mvc): BigQuery por ODBC, cubo de 90 días en disco.
builder.Services.Configure<OpcionesNoSolucion>(builder.Configuration.GetSection(OpcionesNoSolucion.Seccion));
builder.Services.AddSingleton<ServicioNoSolucion>();
builder.Services.AddSingleton(sp => new ServicioCdm(sp.GetRequiredService<ServicioNoSolucion>()));

// GAIA Formación: llamadas de BigQuery filtradas por el Excel de nómina; se recarga sola al cambiar el Excel.
builder.Services.Configure<OpcionesGaia>(builder.Configuration.GetSection(OpcionesGaia.Seccion));
builder.Services.AddSingleton<ServicioGaia>();
builder.Services.AddHostedService<RevisionGaia>();

// Rellamada y No solución por sector: YGMM de BigQuery y Jazztel, Orange y WhatsApp de SQL Server, por mes.
builder.Services.Configure<OpcionesSectores>(builder.Configuration.GetSection(OpcionesSectores.Seccion));
builder.Services.AddSingleton<ServicioSectores>();
builder.Services.AddHostedService<RevisionSectores>();

// Pesos y metas por sector: los bloques de objetivos de los Excel de ranking de incentivos (carpeta compartida).
builder.Services.Configure<OpcionesPesos>(builder.Configuration.GetSection(OpcionesPesos.Seccion));
builder.Services.AddSingleton<ServicioPesos>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/error");
}

app.UseStaticFiles();
app.UseRouting();
app.MapControllers();

app.Run();
