using DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using System.Net.Http;
using System.Threading.Tasks;
using WebMVC.Filtros;

namespace WebMVC.Controllers
{
    [UsuarioLogueado]
    public class ProveedoresController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly string _urlApi;
        private readonly IMemoryCache _cache;

        // Inyección de dependencias para el HttpClientFactory, la configuración y la memoria caché
        public ProveedoresController(IHttpClientFactory httpClientFactory, IConfiguration config, IMemoryCache memoryCache)
        {
            _httpClientFactory = httpClientFactory;
            _urlApi = config.GetValue<string>("UrlAPI") + "Suppliers";
            _cache = memoryCache;
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

        // GET: Proveedores
        public async Task<ActionResult> Index()
        {
            List<ProveedorDTO> proveedores;
            try
            {
                // Intentamos obtener la lista de proveedores de la caché
                if (!_cache.TryGetValue("Proveedores", out proveedores))
                {
                    var client = ConfigureClient();
                    var response = await client.GetAsync(_urlApi);

                    if (response.IsSuccessStatusCode)
                    {
                        proveedores = await response.Content.ReadFromJsonAsync<List<ProveedorDTO>>();
                        // Almacenamos los proveedores en caché con una expiración de 60 minutos
                        _cache.Set("Proveedores", proveedores, new MemoryCacheEntryOptions
                        {
                            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(60)
                        });
                    }
                    else
                    {
                        ModelState.AddModelError("", "Error al obtener los proveedores.");
                        ViewBag.AlertTitle = "Error";
                        ViewBag.AlertIcon = "error";
                        ViewBag.Mensaje = "No se pudieron obtener los proveedores.";
                        return View(new List<ProveedorDTO>());
                    }
                }

                return View(proveedores);
            }
            catch (Exception ex)
            {
                // Captura de cualquier excepción y mensaje de error
                ModelState.AddModelError("", $"Error al obtener los proveedores: {ex.Message}");
                ViewBag.AlertTitle = "Error";
                ViewBag.AlertIcon = "error";
                ViewBag.Mensaje = $"Error al obtener los proveedores: {ex.Message}";
                return View(new List<ProveedorDTO>());
            }
        }

        // GET: Proveedores/Details/5
        public async Task<ActionResult> Details(int id)
        {
            var cacheKey = $"Proveedor_{id}";
            ProveedorDTO proveedor = null;

            try
            {
                // Intentamos obtener el proveedor de la caché
                if (!_cache.TryGetValue(cacheKey, out proveedor))
                {
                    var client = ConfigureClient();
                    var response = await client.GetAsync($"{_urlApi}/{id}");

                    if (response.IsSuccessStatusCode)
                    {
                        proveedor = await response.Content.ReadFromJsonAsync<ProveedorDTO>();

                        // Almacenamos el proveedor en caché con una expiración de 10 minutos
                        _cache.Set(cacheKey, proveedor, new MemoryCacheEntryOptions
                        {
                            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10)
                        });
                    }
                    else
                    {
                        ViewBag.AlertTitle = "Error";
                        ViewBag.AlertIcon = "error";
                        ViewBag.Mensaje = "No se pudo obtener el proveedor.";
                        return NotFound();
                    }
                }
                return View(proveedor);
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"Error al obtener los detalles del proveedor: {ex.Message}");
                ViewBag.AlertTitle = "Error";
                ViewBag.AlertIcon = "error";
                ViewBag.Mensaje = $"Error al obtener los detalles del proveedor: {ex.Message}";
                return NotFound();
            }
        }

        // GET: Proveedores/Create
        public ActionResult Create() => View();

        // POST: Proveedores/Create
        [HttpPost]
        public async Task<ActionResult> Create(ProveedorDTO proveedor)
        {
            try
            {
                var client = ConfigureClient();
                var response = await client.PostAsJsonAsync(_urlApi, proveedor);

                if (response.IsSuccessStatusCode)
                {
                    // Limpiamos la caché ya que los datos han cambiado
                    _cache.Remove("Proveedores");

                    // Agregar mensaje de éxito a ViewBag
                    ViewBag.AlertTitle = "Éxito";
                    ViewBag.AlertIcon = "success";
                    ViewBag.Mensaje = "Proveedor creado exitosamente.";

                    return RedirectToAction(nameof(Index));
                }
                else
                {
                    ModelState.AddModelError(string.Empty, "Error al crear el proveedor.");
                    // Agregar mensaje de error a ViewBag
                    ViewBag.AlertTitle = "Error";
                    ViewBag.AlertIcon = "error";
                    ViewBag.Mensaje = "No se pudo crear el proveedor.";
                    return View(proveedor);
                }
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"Error al crear el proveedor: {ex.Message}");
                // Agregar mensaje de error a ViewBag
                ViewBag.AlertTitle = "Error";
                ViewBag.AlertIcon = "error";
                ViewBag.Mensaje = $"Error al crear el proveedor: {ex.Message}";
                return View(proveedor);
            }
        }

        // GET: Proveedores/Edit/5
        public async Task<ActionResult> Edit(int id)
        {
            var proveedor = await ObtenerProveedorPorId(id);
            if (proveedor == null)
            {
                return NotFound();
            }

            return View(proveedor);
        }

        // POST: Proveedores/Edit/5
        [HttpPost]
        public async Task<ActionResult> Edit(int id, ProveedorDTO proveedor)
        {
            if (id != proveedor.Id)
            {
                return NotFound();
            }
            try
            {
                var client = ConfigureClient();
                var response = await client.PutAsJsonAsync($"{_urlApi}/{id}", proveedor);

                if (response.IsSuccessStatusCode)
                {
                    _cache.Remove("Proveedores");
                    _cache.Remove($"Proveedor_{id}");

                    // Agregar mensaje de éxito a ViewBag
                    ViewBag.AlertTitle = "Éxito";
                    ViewBag.AlertIcon = "success";
                    ViewBag.Mensaje = "Proveedor actualizado exitosamente.";

                    return RedirectToAction(nameof(Index));
                }
                else
                {
                    ModelState.AddModelError(string.Empty, "Error al actualizar el proveedor.");
                    // Agregar mensaje de error a ViewBag
                    ViewBag.AlertTitle = "Error";
                    ViewBag.AlertIcon = "error";
                    ViewBag.Mensaje = "No se pudo actualizar el proveedor.";
                    return View(proveedor);
                }
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"Error al actualizar el proveedor: {ex.Message}");
                // Agregar mensaje de error a ViewBag
                ViewBag.AlertTitle = "Error";
                ViewBag.AlertIcon = "error";
                ViewBag.Mensaje = $"Error al actualizar el proveedor: {ex.Message}";
            }

            return View(proveedor);
        }

        // GET: Proveedores/Delete/5
        public async Task<ActionResult> Delete(int id)
        {
            var proveedor = await ObtenerProveedorPorId(id);
            if (proveedor == null)
            {
                return NotFound();
            }

            return View(proveedor);
        }

        // POST: Proveedores/Delete/5
        [HttpPost, ActionName("Delete")]
        public async Task<ActionResult> DeleteConfirmed(int id)
        {
            try
            {
                var client = ConfigureClient();
                var response = await client.DeleteAsync($"{_urlApi}/{id}");

                if (response.IsSuccessStatusCode)
                {
                    // Limpiamos la caché
                    _cache.Remove("Proveedores");
                    _cache.Remove($"Proveedor_{id}");

                    // Agregar mensaje de éxito a ViewBag
                    ViewBag.AlertTitle = "Éxito";
                    ViewBag.AlertIcon = "success";
                    ViewBag.Mensaje = "Proveedor eliminado exitosamente.";

                    return RedirectToAction(nameof(Index));
                }
                else
                {
                    ModelState.AddModelError("", "Error al eliminar el proveedor.");
                    // Agregar mensaje de error a ViewBag
                    ViewBag.AlertTitle = "Error";
                    ViewBag.AlertIcon = "error";
                    ViewBag.Mensaje = "No se pudo eliminar el proveedor.";
                }
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"Error al eliminar el proveedor: {ex.Message}");
                // Agregar mensaje de error a ViewBag
                ViewBag.AlertTitle = "Error";
                ViewBag.AlertIcon = "error";
                ViewBag.Mensaje = $"Error al eliminar el proveedor: {ex.Message}";
            }

            return View("Index");
        }

        // Método auxiliar para obtener un proveedor por ID, usando la caché
        private async Task<ProveedorDTO> ObtenerProveedorPorId(int id)
        {
            var cacheKey = $"Proveedor_{id}";
            ProveedorDTO proveedor = null;

            try
            {
                // Intentamos obtener el proveedor de la caché
                if (!_cache.TryGetValue(cacheKey, out proveedor))
                {
                    var client = ConfigureClient();
                    var response = await client.GetAsync($"{_urlApi}/{id}");

                    if (response.IsSuccessStatusCode)
                    {
                        proveedor = await response.Content.ReadFromJsonAsync<ProveedorDTO>();

                        // Almacenamos el proveedor en caché con una expiración de 10 minutos
                        _cache.Set(cacheKey, proveedor, new MemoryCacheEntryOptions
                        {
                            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10)
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                // Captura de cualquier excepción
                ModelState.AddModelError("", $"Error al obtener el proveedor por ID: {ex.Message}");
            }

            return proveedor;
        }
    }
}
