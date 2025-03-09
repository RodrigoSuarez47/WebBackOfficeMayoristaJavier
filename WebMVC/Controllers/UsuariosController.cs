using DTOs;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using WebMVC.ClasesAuxiliares;

namespace WebMVC.Controllers
{
    public class UsuariosController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly string _urlApi;
        public UsuariosController(IHttpClientFactory httpClientFactory, IConfiguration config)
        {
            _httpClientFactory = httpClientFactory;
            _urlApi = config.GetValue<string>("UrlAPI") + "Users";
        }
        private HttpClient ConfigureClient()
        {
            var client = _httpClientFactory.CreateClient();
            var token = HttpContext.Session.GetString("Token");

            if (!string.IsNullOrEmpty(token))
            {
                client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            }

            return client;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            try
            {
                var client = ConfigureClient();
                using var response = await client.GetAsync(_urlApi);

                if (response.IsSuccessStatusCode)
                {
                    var usuarios = JsonConvert.DeserializeObject<List<UsuarioDTO>>(await response.Content.ReadAsStringAsync());
                    return View(usuarios);
                }
            }
            catch
            {
                ViewBag.Mensaje = "Error al obtener la lista de usuarios.";
            }

            return View(new List<UsuarioDTO>());
        }

        [HttpGet]
        public IActionResult Create() => View();

        [HttpPost]
        public async Task<IActionResult> Create(UsuarioDTO usuario)
        {
            if (ModelState.IsValid)
            {
                var client = ConfigureClient();
                using var response = await client.PostAsJsonAsync($"{_urlApi}/AddUser", usuario);

                if (response.IsSuccessStatusCode)
                    return RedirectToAction("Index");

                ViewBag.Mensaje = "Error al crear el usuario.";
            }
            return View(usuario);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            try
            {
                var client = ConfigureClient();
                using var response = await client.GetAsync($"{_urlApi}/GetUserById/{id}");

                if (response.IsSuccessStatusCode)
                {
                    var usuario = JsonConvert.DeserializeObject<UsuarioDTO>(await response.Content.ReadAsStringAsync());
                    return View(usuario);
                }
            }
            catch
            {
                ViewBag.Mensaje = "Error al obtener el usuario.";
            }

            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> Edit(UsuarioDTO usuario)
        {
            if (ModelState.IsValid)
            {
                var client = ConfigureClient();
                using var response = await client.PutAsJsonAsync($"{_urlApi}/UpdateUser/{usuario.Id}", usuario);

                if (response.IsSuccessStatusCode)
                    return RedirectToAction("Index");

                ViewBag.Mensaje = "Error al editar el usuario.";
            }
            return View(usuario);
        }

        [HttpPost, ActionName("Delete")]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var client = ConfigureClient();
                using var response = await client.DeleteAsync($"{_urlApi}/DeleteUser/{id}");

                if (response.IsSuccessStatusCode)
                    return RedirectToAction("Index");

                ViewBag.Mensaje = "Error al eliminar el usuario.";
            }
            catch
            {
                ViewBag.Mensaje = "Error interno al eliminar el usuario.";
            }

            return RedirectToAction("Index");
        }

        [HttpGet]
        public IActionResult Login() => View();

        [HttpPost]
        public async Task<IActionResult> Login(LoginDTO usuario)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    var client = ConfigureClient();
                    using var response = await client.PostAsJsonAsync($"{_urlApi}/Login", usuario);

                    var cuerpo = await response.Content.ReadAsStringAsync();
                    if (response.IsSuccessStatusCode)
                    {
                        var usuarioLogueado = JsonConvert.DeserializeObject<UsuarioLogueadoDTO>(cuerpo);
                        if (usuarioLogueado != null)
                        {
                            HttpContext.Session.SetString("Token", usuarioLogueado.Token);
                            HttpContext.Session.SetString("Nombre", usuarioLogueado.Name);
                            Console.WriteLine("Token: " + HttpContext.Session.GetString("Token"));

                            return RedirectToAction("List", "Articulos");
                        }
                        ViewBag.Mensaje = "Email o Contraseña incorrectos";
                    }
                    else
                    {
                        ViewBag.Mensaje = cuerpo;
                    }
                }
                else
                {
                    ViewBag.Mensaje = "Error, debe ingresar Email y Contraseña";
                }
            }
            catch
            {
                ViewBag.Mensaje = "Error interno. Inténtelo más tarde";
            }

            return View("Login");
        }

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
