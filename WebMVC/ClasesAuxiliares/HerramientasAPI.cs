using DTOs;
using Newtonsoft.Json;
using System.Text.Json;
using WebMVC.Controllers;

namespace WebMVC.ClasesAuxiliares
{
    public class HerramientasAPI
    {
        public static string LeerContenidoRespuesta(HttpResponseMessage respuesta)
        {
            HttpContent content = respuesta.Content;
            var tarea2 = content.ReadAsStringAsync();
            tarea2.Wait();
            string cuerpo = tarea2.Result;
            return cuerpo;
        }

        public static UsuarioDTO CrearUsuarioDTO(HttpResponseMessage respuesta)
        {
            HttpContent content = respuesta.Content;
            var tarea2 = content.ReadAsStringAsync();
            tarea2.Wait();
            string cuerpo = tarea2.Result;
            if (string.IsNullOrEmpty(cuerpo))
            {
                throw new Exception("El contenido de la respuesta está vacío.");
            }
            UsuarioDTO usuario = JsonConvert.DeserializeObject<UsuarioDTO>(cuerpo);
            return usuario;
        }
    }
}
