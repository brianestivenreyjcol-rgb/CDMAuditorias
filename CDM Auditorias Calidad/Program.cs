using System.Globalization;
using CDM_Auditorias_Calidad.Servicios.Configuracion;
using CDM_Auditorias_Calidad.Servicios.Datos;

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

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/error");
}

app.UseStaticFiles();
app.UseRouting();
app.MapControllers();

app.Run();
