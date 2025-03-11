using DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using WebMVC.Filtros;

namespace WebMVC.Controllers
{
    public class UsuariosController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly string _urlApi;

        public UsuariosController(IHttpClientFactory httpClientFactory, IConfiguration config)
        {
            _httpClientFactory = httpClientFactory;
            _urlApi = config.GetValue<string>("UrlAPI") + "users"; // Cambio para reflejar la nueva ruta RESTful
        }

        private HttpClient ConfigureClient()
        {
            var client = _httpClientFactory.CreateClient();
            var token = HttpContext.Session.GetString("Token");

            // Solo incluir el token si está presente y es necesario
            if (!string.IsNullOrEmpty(token))
            {
                client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            }

            return client;
        }

        [UsuarioLogueado]
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            try
            {
                var client = ConfigureClient();
                using var response = await client.GetAsync(_urlApi);

                if (response.IsSuccessStatusCode)
                {
                    var usuarios = await response.Content.ReadFromJsonAsync<List<UsuarioDTO>>();
                    return View(usuarios);
                }
                else
                {
                    var message = await response.Content.ReadAsStringAsync();
                    ViewBag.Mensaje = string.IsNullOrEmpty(message) ? "Error desconocido" : message;
                    return View("Error");
                }
            }
            catch
            {
                ViewBag.Mensaje = "Error al obtener la lista de usuarios.";
                return View(new List<UsuarioDTO>());
            }
        }

        [UsuarioLogueado]
        [HttpGet]
        public IActionResult Create() => View();

        [UsuarioLogueado]
        [HttpPost]
        public async Task<IActionResult> Create(UsuarioDTO usuario)
        {
            if (ModelState.IsValid)
            {
                var client = ConfigureClient();
                using var response = await client.PostAsJsonAsync(_urlApi, usuario); // POST directamente en /users

                if (response.IsSuccessStatusCode)
                {
                    return RedirectToAction("Index");
                }
                else
                {
                    var message = await response.Content.ReadAsStringAsync();
                    ViewBag.Mensaje = string.IsNullOrEmpty(message) ? "Error desconocido" : message;
                    return View(usuario);
                }
            }

            return View(usuario);
        }

        [UsuarioLogueado]
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            try
            {
                var client = ConfigureClient();
                using var response = await client.GetAsync($"{_urlApi}/{id}"); // GET en /users/{id}

                if (response.IsSuccessStatusCode)
                {
                    var usuario = await response.Content.ReadFromJsonAsync<UsuarioDTO>();
                    return View(usuario);
                }

                var message = await response.Content.ReadAsStringAsync();
                ViewBag.Mensaje = string.IsNullOrEmpty(message) ? "Error desconocido" : message;
                return View("Error");
            }
            catch
            {
                ViewBag.Mensaje = "Error al obtener el usuario.";
                return RedirectToAction("Index");
            }
        }


        [UsuarioLogueado]
        [HttpPost]
        public async Task<IActionResult> Edit(UsuarioDTO usuario)
        {
            if (ModelState.IsValid)
            {
                var client = ConfigureClient();
                using var response = await client.PutAsJsonAsync($"{_urlApi}/{usuario.Id}", usuario); // PUT en /users/{id}

                if (response.IsSuccessStatusCode)
                {
                    return RedirectToAction("Index");
                }
                else
                {
                    var message = await response.Content.ReadAsStringAsync();
                    ViewBag.Mensaje = string.IsNullOrEmpty(message) ? "Error desconocido" : message;
                    return View(usuario);
                }
            }

            return View(usuario);
        }

        [UsuarioLogueado]
        [HttpPost, ActionName("Delete")]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var client = ConfigureClient();
                using var response = await client.DeleteAsync($"{_urlApi}/{id}"); // DELETE en /users/{id}

                if (response.IsSuccessStatusCode)
                {
                    return RedirectToAction("Index");
                }

                var message = await response.Content.ReadAsStringAsync();
                ViewBag.Mensaje = string.IsNullOrEmpty(message) ? "Error desconocido" : message;
                return View("Error");
            }
            catch
            {
                ViewBag.Mensaje = "Error interno al eliminar el usuario.";
                return RedirectToAction("Index");
            }
        }

        [HttpGet]
        public IActionResult Login() => View();

        [HttpPost]
        public async Task<IActionResult> Login(LoginDTO usuario)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Mensaje = "Error, debe ingresar Email y Contraseña";
                return View("Login");
            }

            try
            {
                var client = ConfigureClient();
                using var response = await client.PostAsJsonAsync($"{_urlApi}/login", usuario); // POST en /users/login

                if (response.IsSuccessStatusCode)
                {
                    var usuarioLogueado = await response.Content.ReadFromJsonAsync<UsuarioLogueadoDTO>();
                    if (usuarioLogueado != null)
                    {
                        HttpContext.Session.SetString("Token", usuarioLogueado.Token);
                        HttpContext.Session.SetString("Nombre", usuarioLogueado.Name);
                        return RedirectToAction("List", "Articulos");
                    }
                    ViewBag.Mensaje = "Email o Contraseña incorrectos";
                }
                else
                {
                    var cuerpo = await response.Content.ReadAsStringAsync();
                    ViewBag.Mensaje = cuerpo;
                }
            }
            catch (Exception ex)
            {
                ViewBag.Mensaje = "Error interno. Inténtelo más tarde";
            }

            return View("Login");
        }

        [UsuarioLogueado]
        [HttpPost]
        public IActionResult Logout()
        {
            try
            {
                if (!string.IsNullOrEmpty(HttpContext.Session.GetString("Token")))
                {
                    HttpContext.Session.Clear();
                    return RedirectToAction("Login");
                }

                ViewBag.Mensaje = "Error al cerrar sesión";
            }
            catch
            {
                ViewBag.Mensaje = "Error interno. Inténtelo más tarde.";
            }

            return RedirectToAction("Index");
        }
    }
}
