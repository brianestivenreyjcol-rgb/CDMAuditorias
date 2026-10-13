using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace CDM_Auditorias_Calidad.Servicios.Acceso;

/// <summary>
/// Cierra con la contraseña de «Personal» (<see cref="AccesoPersonal"/>) un controlador o una acción: sin un pase vigente,
/// lleva a <c>/personal/entrar</c> y, al acertar, vuelve a la misma dirección. Va en el grupo «Personal» de la portada y en
/// sus informes (Sectores y Pesos), también en sus CSV y botones, para que no se pueda entrar escribiendo la dirección.
/// </summary>
/// <remarks>
/// En las recargas parciales (cabecera <c>X-Parcial</c>) site.js sigue la redirección, no encuentra <c>#informe</c> y
/// recarga la página entera, que vuelve a pasar por aquí: así un pase que caduca a mitad de uso acaba en la contraseña.
/// </remarks>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class AccesoPersonalAttribute : Attribute, IAuthorizationFilter
{
    public void OnAuthorization(AuthorizationFilterContext context)
    {
        var http = context.HttpContext;
        if (http.RequestServices.GetRequiredService<AccesoPersonal>().PaseValido(http)) return;

        // Una petición POST (Actualizar, Analizar) vuelve a la página de su informe, no a la del botón.
        var volver = HttpMethods.IsGet(http.Request.Method)
            ? http.Request.PathBase + http.Request.Path + http.Request.QueryString
            : "/personal";
        context.Result = new RedirectResult("/personal/entrar?volver=" + Uri.EscapeDataString(volver));
    }
}
