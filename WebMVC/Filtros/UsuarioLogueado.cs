using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Controllers;
using System;

namespace WebMVC.Filtros
{
    public class UsuarioLogueado : Attribute, IAuthorizationFilter
    {
        public void OnAuthorization(AuthorizationFilterContext context)
        {
            // Obtener el nombre del controlador y la acción actual
            var descriptor = context.ActionDescriptor as ControllerActionDescriptor;
            string controller = descriptor?.ControllerName ?? "";
            string action = descriptor?.ActionName ?? "";

            // Permitir acceso sin autenticación a Login y Registro
            if (controller.Equals("Usuarios", StringComparison.OrdinalIgnoreCase) &&
                (action.Equals("Login", StringComparison.OrdinalIgnoreCase)))
            {
                return;
            }

            // Verificar si hay un token en la sesión
            string token = context.HttpContext.Session.GetString("Token");
            if (string.IsNullOrEmpty(token))
            {
                context.Result = new RedirectToActionResult("Login", "Usuarios", null);
            }
        }
    }
}
