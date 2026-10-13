using CDM_Auditorias_Calidad.Servicios.Acceso;
using Microsoft.AspNetCore.DataProtection;

namespace CDM_Auditorias_Calidad.Tests;

/// <summary>La contraseña del grupo «Personal» de la portada y su pase de 12 h.</summary>
public class AccesoPersonalTests
{
    private const string Ip = "10.0.0.1";
    private readonly EphemeralDataProtectionProvider _proteccion = new();
    private string? _clave = "clave-de-prueba";
    // Data Protection también mira la hora real al abrir el pase: la de la prueba parte de ahora.
    private DateTimeOffset _ahora = DateTimeOffset.UtcNow;

    private AccesoPersonal Nuevo() => new(_proteccion, () => _clave, () => _ahora);

    [Fact]
    public void Con_la_contrasena_correcta_da_un_pase_valido()
    {
        var acceso = Nuevo();
        var pase = acceso.Entrar("clave-de-prueba", Ip);
        Assert.NotNull(pase);
        Assert.True(acceso.PaseValido(pase));
    }

    [Theory]
    [InlineData("clave-de-prueba-2")]
    [InlineData("4321")]
    [InlineData("")]
    [InlineData(null)]
    public void Con_otra_contrasena_no_hay_pase(string? intento)
        => Assert.Null(Nuevo().Entrar(intento, Ip));

    [Fact]
    public void Sin_pase_o_con_uno_manipulado_no_se_entra()
    {
        var acceso = Nuevo();
        Assert.False(acceso.PaseValido((string?)null));
        Assert.False(acceso.PaseValido("clave-de-prueba"));
        var pase = acceso.Entrar("clave-de-prueba", Ip)!;
        Assert.False(acceso.PaseValido(pase[..^2] + (pase[^2..] == "AA" ? "BB" : "AA")));
    }

    [Fact]
    public void El_pase_caduca_a_las_12_horas()
    {
        var acceso = Nuevo();
        var pase = acceso.Entrar("clave-de-prueba", Ip);
        _ahora += AccesoPersonal.Duracion - TimeSpan.FromMinutes(1);
        Assert.True(acceso.PaseValido(pase));
        _ahora += TimeSpan.FromMinutes(2);
        Assert.False(acceso.PaseValido(pase));
    }

    [Fact]
    public void Cambiar_la_contrasena_cierra_los_pases_ya_dados()
    {
        var acceso = Nuevo();
        var pase = acceso.Entrar("clave-de-prueba", Ip);
        _clave = "9999";
        Assert.False(acceso.PaseValido(pase));
    }

    [Fact]
    public void Sin_contrasena_configurada_nadie_entra()
    {
        _clave = null;
        var acceso = Nuevo();
        Assert.False(acceso.Configurada);
        Assert.Null(acceso.Entrar("", Ip));
        Assert.Null(acceso.Entrar("clave-de-prueba", Ip));
    }

    [Fact]
    public void Tras_cinco_fallos_la_ip_espera_aunque_acierte()
    {
        var acceso = Nuevo();
        for (var i = 0; i < AccesoPersonal.FallosMaximos; i++) Assert.Null(acceso.Entrar("otra", Ip));
        Assert.True(acceso.Bloqueada(Ip));
        Assert.Null(acceso.Entrar("clave-de-prueba", Ip));
        Assert.NotNull(acceso.Entrar("clave-de-prueba", "10.0.0.2")); // Otra IP no espera.

        _ahora += AccesoPersonal.Espera + TimeSpan.FromSeconds(1);
        Assert.False(acceso.Bloqueada(Ip));
        Assert.NotNull(acceso.Entrar("clave-de-prueba", Ip));
    }

    [Fact]
    public void Acertar_pone_a_cero_los_fallos()
    {
        var acceso = Nuevo();
        for (var i = 0; i < AccesoPersonal.FallosMaximos - 1; i++) acceso.Entrar("otra", Ip);
        Assert.NotNull(acceso.Entrar("clave-de-prueba", Ip));
        acceso.Entrar("otra", Ip);
        Assert.False(acceso.Bloqueada(Ip));
    }
}
