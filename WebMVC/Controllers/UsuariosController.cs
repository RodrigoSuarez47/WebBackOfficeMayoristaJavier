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
                    ViewBag.AlertIcon = "error";
                    ViewBag.AlertTitle = "Error";
                    return View("Error");
                }
            }
            catch (Exception ex)
            {
                ViewBag.Mensaje = $"Error al obtener la lista de usuarios: {ex.Message}";
                ViewBag.AlertIcon = "error";
                ViewBag.AlertTitle = "Error";
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
                try
                {
                    var client = ConfigureClient();
                    using var response = await client.PostAsJsonAsync(_urlApi, usuario); // POST directamente en /users

                    if (response.IsSuccessStatusCode)
                    {
                        ViewBag.Mensaje = "Usuario creado con éxito";
                        ViewBag.AlertIcon = "success";
                        ViewBag.AlertTitle = "¡Éxito!";
                        return RedirectToAction("Index");
                    }
                    else
                    {
                        var message = await response.Content.ReadAsStringAsync();
                        ViewBag.Mensaje = string.IsNullOrEmpty(message) ? "Error desconocido" : message;
                        ViewBag.AlertIcon = "error";
                        ViewBag.AlertTitle = "Error";
                        return View(usuario);
                    }
                }
                catch (Exception ex)
                {
                    ViewBag.Mensaje = $"Error al crear el usuario: {ex.Message}";
                    ViewBag.AlertIcon = "error";
                    ViewBag.AlertTitle = "Error";
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
                ViewBag.AlertIcon = "error";
                ViewBag.AlertTitle = "Error";
                return View("Error");
            }
            catch (Exception ex)
            {
                ViewBag.Mensaje = $"Error al obtener el usuario: {ex.Message}";
                ViewBag.AlertIcon = "error";
                ViewBag.AlertTitle = "Error";
                return RedirectToAction("Index");
            }
        }

        [UsuarioLogueado]
        [HttpPost]
        public async Task<IActionResult> Edit(UsuarioDTO usuario)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var client = ConfigureClient();
                    using var response = await client.PutAsJsonAsync($"{_urlApi}/{usuario.Id}", usuario); // PUT en /users/{id}

                    if (response.IsSuccessStatusCode)
                    {
                        ViewBag.Mensaje = "Usuario actualizado con éxito";
                        ViewBag.AlertIcon = "success";
                        ViewBag.AlertTitle = "¡Éxito!";
                        return RedirectToAction("Index");
                    }
                    else
                    {
                        var message = await response.Content.ReadAsStringAsync();
                        ViewBag.Mensaje = string.IsNullOrEmpty(message) ? "Error desconocido" : message;
                        ViewBag.AlertIcon = "error";
                        ViewBag.AlertTitle = "Error";
                        return View(usuario);
                    }
                }
                catch (Exception ex)
                {
                    ViewBag.Mensaje = $"Error al actualizar el usuario: {ex.Message}";
                    ViewBag.AlertIcon = "error";
                    ViewBag.AlertTitle = "Error";
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
                    ViewBag.Mensaje = "Usuario eliminado con éxito";
                    ViewBag.AlertIcon = "success";
                    ViewBag.AlertTitle = "¡Éxito!";
                    return RedirectToAction("Index");
                }

                var message = await response.Content.ReadAsStringAsync();
                ViewBag.Mensaje = string.IsNullOrEmpty(message) ? "Error desconocido" : message;
                ViewBag.AlertIcon = "error";
                ViewBag.AlertTitle = "Error";
                return View("Error");
            }
            catch (Exception ex)
            {
                ViewBag.Mensaje = $"Error interno al eliminar el usuario: {ex.Message}";
                ViewBag.AlertIcon = "error";
                ViewBag.AlertTitle = "Error";
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
                ViewBag.Mensaje = "Error, debe ingresar Usuario y Contraseña";
                ViewBag.AlertIcon = "error";
                ViewBag.AlertTitle = "Error";
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
                }
                else
                {
                    ViewBag.Mensaje = "Usuario o Contraseña incorrectos";
                    ViewBag.AlertIcon = "error";
                    ViewBag.AlertTitle = "Error";
                    return View("Login");
                }
            }
            catch (Exception ex)
            {
                ViewBag.Mensaje = $"Error interno al iniciar sesión: {ex.Message}";
                ViewBag.AlertIcon = "error";
                ViewBag.AlertTitle = "Error";
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
                    ViewBag.Mensaje = "Sesión cerrada con éxito";
                    ViewBag.AlertIcon = "success";
                    ViewBag.AlertTitle = "¡Éxito!";
                    return RedirectToAction("Login");
                }

                ViewBag.Mensaje = "Error al cerrar sesión";
                ViewBag.AlertIcon = "error";
                ViewBag.AlertTitle = "Error";
            }
            catch (Exception ex)
            {
                ViewBag.Mensaje = $"Error interno al cerrar sesión: {ex.Message}";
                ViewBag.AlertIcon = "error";
                ViewBag.AlertTitle = "Error";
            }

            return RedirectToAction("Index");
        }
    }
}
