using DTOs;
using Microsoft.AspNetCore.Mvc;
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
            _urlApi = config.GetValue<string>("UrlAPI") + "users";
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

        private async Task<List<UsuarioDTO>> ObtenerUsuariosAsync()
        {
            var client = ConfigureClient();
            using var response = await client.GetAsync(_urlApi);

            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<List<UsuarioDTO>>() ?? new List<UsuarioDTO>();
            }

            return new List<UsuarioDTO>();
        }

        [UsuarioLogueado]
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            try
            {
                var usuarios = await ObtenerUsuariosAsync();
                return View(usuarios);
            }
            catch (Exception ex)
            {
                TempData["Mensaje"] = $"Error al obtener la lista de usuarios: {ex.Message}";
                TempData["AlertIcon"] = "error";
                TempData["AlertTitle"] = "Error";
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
            if (!ModelState.IsValid) return View(usuario);

            try
            {
                var client = ConfigureClient();
                using var response = await client.PostAsJsonAsync(_urlApi, usuario);

                if (response.IsSuccessStatusCode)
                {
                    TempData["Mensaje"] = "Usuario creado con éxito";
                    TempData["AlertIcon"] = "success";
                    TempData["AlertTitle"] = "¡Éxito!";
                    return RedirectToAction("Index");
                }

                ViewBag.Mensaje = await response.Content.ReadAsStringAsync() ?? "Error desconocido";
                ViewBag.AlertIcon = "error";
                ViewBag.AlertTitle = "Error";
            }
            catch (Exception ex)
            {
                ViewBag.Mensaje = $"Error al crear el usuario: {ex.Message}";
                ViewBag.AlertIcon = "error";
                ViewBag.AlertTitle = "Error";
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
                using var response = await client.GetAsync($"{_urlApi}/{id}");

                if (response.IsSuccessStatusCode)
                {
                    var usuario = await response.Content.ReadFromJsonAsync<UsuarioDTO>();
                    return View(usuario);
                }

                TempData["Mensaje"] = "No se pudo obtener el usuario";
                TempData["AlertIcon"] = "error";
                TempData["AlertTitle"] = "Error";
            }
            catch (Exception ex)
            {
                TempData["Mensaje"] = $"Error al obtener el usuario: {ex.Message}";
                TempData["AlertIcon"] = "error";
                TempData["AlertTitle"] = "Error";
            }

            return RedirectToAction("Index");
        }

        [UsuarioLogueado]
        [HttpPost]
        public async Task<IActionResult> Edit(UsuarioDTO usuario)
        {
            if (!ModelState.IsValid) return View(usuario);

            try
            {
                var client = ConfigureClient();
                using var response = await client.PutAsJsonAsync($"{_urlApi}/{usuario.Id}", usuario);

                if (response.IsSuccessStatusCode)
                {
                    TempData["Mensaje"] = "Usuario actualizado con éxito";
                    TempData["AlertIcon"] = "success";
                    TempData["AlertTitle"] = "¡Éxito!";
                    return RedirectToAction("Index");
                }

                ViewBag.Mensaje = await response.Content.ReadAsStringAsync() ?? "Error desconocido";
                ViewBag.AlertIcon = "error";
                ViewBag.AlertTitle = "Error";
            }
            catch (Exception ex)
            {
                ViewBag.Mensaje = $"Error al actualizar el usuario: {ex.Message}";
                ViewBag.AlertIcon = "error";
                ViewBag.AlertTitle = "Error";
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
                using var response = await client.DeleteAsync($"{_urlApi}/{id}");

                if (response.IsSuccessStatusCode)
                {
                    TempData["Mensaje"] = "Usuario eliminado con éxito";
                    TempData["AlertIcon"] = "success";
                    TempData["AlertTitle"] = "¡Éxito!";
                }
                else
                {
                    TempData["Mensaje"] = "No se pudo eliminar el usuario";
                    TempData["AlertIcon"] = "error";
                    TempData["AlertTitle"] = "Error";
                }
            }
            catch (Exception ex)
            {
                TempData["Mensaje"] = $"Error interno al eliminar el usuario: {ex.Message}";
                TempData["AlertIcon"] = "error";
                TempData["AlertTitle"] = "Error";
            }

            return RedirectToAction("Index");
        }

        [HttpGet]
        public IActionResult Login() => View();

        [HttpPost]
        public async Task<IActionResult> Login(LoginDTO usuario)
        {
            if (!ModelState.IsValid)
            {
                TempData["Mensaje"] = "Debe ingresar usuario y contraseña";
                TempData["AlertIcon"] = "error";
                TempData["AlertTitle"] = "Error";
                return View("Login");
            }

            try
            {
                var client = ConfigureClient();
                using var response = await client.PostAsJsonAsync($"{_urlApi}/login", usuario);

                if (response.IsSuccessStatusCode)
                {
                    var usuarioLogueado = await response.Content.ReadFromJsonAsync<UsuarioLogueadoDTO>();
                    if (usuarioLogueado != null)
                    {
                        HttpContext.Session.SetString("Token", usuarioLogueado.Token);
                        HttpContext.Session.SetString("Nombre", usuarioLogueado.Name);
                        return RedirectToAction("List", "Articulos");
                    }
                }

                TempData["Mensaje"] = "Usuario o contraseña incorrectos";
                TempData["AlertIcon"] = "error";
                TempData["AlertTitle"] = "Error";
            }
            catch (Exception ex)
            {
                TempData["Mensaje"] = $"Error interno al iniciar sesión: {ex.Message}";
                TempData["AlertIcon"] = "error";
                TempData["AlertTitle"] = "Error";
            }

            return View("Login");
        }

        [UsuarioLogueado]
        [HttpPost]
        public IActionResult Logout()
        {
            try
            {
                HttpContext.Session.Clear();
                TempData["Mensaje"] = "Sesión cerrada con éxito";
                TempData["AlertIcon"] = "success";
                TempData["AlertTitle"] = "¡Éxito!";
            }
            catch (Exception ex)
            {
                TempData["Mensaje"] = $"Error interno al cerrar sesión: {ex.Message}";
                TempData["AlertIcon"] = "error";
                TempData["AlertTitle"] = "Error";
            }

            return RedirectToAction("Login");
        }
    }
}
