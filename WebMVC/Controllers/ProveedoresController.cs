using DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
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
        [UsuarioLogueado]
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
                        TempData["AlertTitle"] = "Error";
                        TempData["AlertIcon"] = "error";
                        TempData["Mensaje"] = "No se pudieron obtener los proveedores.";
                        return View(new List<ProveedorDTO>());
                    }
                }

                return View(proveedores);
            }
            catch (Exception ex)
            {
                // Captura de cualquier excepción y mensaje de error
                TempData["AlertTitle"] = "Error";
                TempData["AlertIcon"] = "error";
                TempData["Mensaje"] = $"Error al obtener los proveedores: {ex.Message}";
                return View(new List<ProveedorDTO>());
            }
        }

        // GET: Proveedores/Details/5
        [UsuarioLogueado]
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
                        TempData["AlertTitle"] = "Error";
                        TempData["AlertIcon"] = "error";
                        TempData["Mensaje"] = "No se pudo obtener el proveedor.";
                        return NotFound();
                    }
                }
                return View(proveedor);
            }
            catch (Exception ex)
            {
                TempData["AlertTitle"] = "Error";
                TempData["AlertIcon"] = "error";
                TempData["Mensaje"] = $"Error al obtener los detalles del proveedor: {ex.Message}";
                return NotFound();
            }
        }

        // GET: Proveedores/Create
        [UsuarioLogueado]
        public ActionResult Create() => View();

        [UsuarioLogueado]
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

                    // Agregar mensaje de éxito a TempData
                    TempData["AlertTitle"] = "Éxito";
                    TempData["AlertIcon"] = "success";
                    TempData["Mensaje"] = "Proveedor creado exitosamente.";

                    return RedirectToAction(nameof(Index));
                }
                else
                {
                    TempData["AlertTitle"] = "Error";
                    TempData["AlertIcon"] = "error";
                    TempData["Mensaje"] = "No se pudo crear el proveedor.";
                    return View(proveedor);
                }
            }
            catch (Exception ex)
            {
                TempData["AlertTitle"] = "Error";
                TempData["AlertIcon"] = "error";
                TempData["Mensaje"] = $"Error al crear el proveedor: {ex.Message}";
                return View(proveedor);
            }
        }

        // GET: Proveedores/Edit/5
        [UsuarioLogueado]
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
        [UsuarioLogueado]
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

                    // Agregar mensaje de éxito a TempData
                    TempData["AlertTitle"] = "Éxito";
                    TempData["AlertIcon"] = "success";
                    TempData["Mensaje"] = "Proveedor actualizado exitosamente.";

                    return RedirectToAction(nameof(Index));
                }
                else
                {
                    TempData["AlertTitle"] = "Error";
                    TempData["AlertIcon"] = "error";
                    TempData["Mensaje"] = "No se pudo actualizar el proveedor.";
                    return View(proveedor);
                }
            }
            catch (Exception ex)
            {
                TempData["AlertTitle"] = "Error";
                TempData["AlertIcon"] = "error";
                TempData["Mensaje"] = $"Error al actualizar el proveedor: {ex.Message}";
            }

            return View(proveedor);
        }

        // GET: Proveedores/Delete/5
        [UsuarioLogueado]
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
        [UsuarioLogueado]
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

                    // Agregar mensaje de éxito a TempData
                    TempData["AlertTitle"] = "Éxito";
                    TempData["AlertIcon"] = "success";
                    TempData["Mensaje"] = "Proveedor eliminado exitosamente.";

                    return RedirectToAction(nameof(Index));
                }
                else
                {
                    TempData["AlertTitle"] = "Error";
                    TempData["AlertIcon"] = "error";
                    TempData["Mensaje"] = "No se pudo eliminar el proveedor.";
                }
            }
            catch (Exception ex)
            {
                TempData["AlertTitle"] = "Error";
                TempData["AlertIcon"] = "error";
                TempData["Mensaje"] = $"Error al eliminar el proveedor: {ex.Message}";
            }

            return View("Index");
        }

        // Método auxiliar para obtener un proveedor por ID, usando la caché
        [UsuarioLogueado]
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
                TempData["AlertTitle"] = "Error";
                TempData["AlertIcon"] = "error";
                TempData["Mensaje"] = $"Error al obtener el proveedor por ID: {ex.Message}";
            }

            return proveedor;
        }
    }
}
