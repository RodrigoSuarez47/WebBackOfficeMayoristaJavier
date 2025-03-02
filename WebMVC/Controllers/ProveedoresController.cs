using DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using System.Net.Http;
using System.Threading.Tasks;


namespace WebMVC.Controllers
{
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

        // GET: Proveedores
        public async Task<ActionResult> Index()
        {
            if (!_cache.TryGetValue("Proveedores", out List<ProveedorDTO> proveedores))
            {
                var client = _httpClientFactory.CreateClient();
                var response = await client.GetAsync(_urlApi);

                if (response.IsSuccessStatusCode)
                {
                    proveedores = await response.Content.ReadFromJsonAsync<List<ProveedorDTO>>();
                    _cache.Set("Proveedores", proveedores, new MemoryCacheEntryOptions
                    {
                        AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(60)
                    });
                }
            }
            return View(proveedores);
        }

        // GET: Proveedores/Details/5
        public async Task<ActionResult> Details(int id)
        {
            var cacheKey = $"Proveedor_{id}";
            ProveedorDTO proveedor = null;

            // Intentamos obtener el proveedor de la caché
            if (!_cache.TryGetValue(cacheKey, out proveedor))
            {
                var client = _httpClientFactory.CreateClient();
                var response = await client.GetAsync($"{_urlApi}/{id}");

                if (response.IsSuccessStatusCode)
                {
                    proveedor = await response.Content.ReadFromJsonAsync<ProveedorDTO>();

                    // Almacenamos el proveedor en caché
                    _cache.Set(cacheKey, proveedor, new MemoryCacheEntryOptions
                    {
                        AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10)
                    });
                }
                else
                {
                    return NotFound();
                }
            }

            return View(proveedor);
        }

        // GET: Proveedores/Create
        public ActionResult Create()
        {
            return View();
        }

        // POST: Proveedores/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Create(ProveedorDTO proveedor)
        {
            if (ModelState.IsValid)
            {
                var client = _httpClientFactory.CreateClient();
                var response = await client.PostAsJsonAsync(_urlApi, proveedor);

                if (response.IsSuccessStatusCode)
                {
                    // Limpiamos la caché ya que los datos han cambiado
                    _cache.Remove("Proveedores");
                    return RedirectToAction(nameof(Index));
                }
                else
                {
                    // Manejo de error
                    ModelState.AddModelError(string.Empty, "Error al crear el proveedor.");
                }
            }
            return View(proveedor);
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
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit(int id, ProveedorDTO proveedor)
        {
            if (id != proveedor.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                var client = _httpClientFactory.CreateClient();
                var response = await client.PutAsJsonAsync($"{_urlApi}/{id}", proveedor);

                if (response.IsSuccessStatusCode)
                {
                    // Limpiamos la caché ya que los datos han cambiado
                    _cache.Remove("Proveedores");
                    _cache.Remove($"Proveedor_{id}");
                    return RedirectToAction(nameof(Index));
                }
                else
                {
                    ModelState.AddModelError(string.Empty, "Error al actualizar el proveedor.");
                }
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
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> DeleteConfirmed(int id)
        {
            var client = _httpClientFactory.CreateClient();
            var response = await client.DeleteAsync($"{_urlApi}/{id}");

            if (response.IsSuccessStatusCode)
            {
                // Limpiamos la caché
                _cache.Remove("Proveedores");
                _cache.Remove($"Proveedor_{id}");
                return RedirectToAction(nameof(Index));
            }

            return View();
        }

        // Método auxiliar para obtener un proveedor por ID, usando la caché
        private async Task<ProveedorDTO> ObtenerProveedorPorId(int id)
        {
            var cacheKey = $"Proveedor_{id}";
            ProveedorDTO proveedor = null;

            // Intentamos obtener el proveedor de la caché
            if (!_cache.TryGetValue(cacheKey, out proveedor))
            {
                var client = _httpClientFactory.CreateClient();
                var response = await client.GetAsync($"{_urlApi}/{id}");

                if (response.IsSuccessStatusCode)
                {
                    proveedor = await response.Content.ReadFromJsonAsync<ProveedorDTO>();

                    // Almacenamos el proveedor en caché
                    _cache.Set(cacheKey, proveedor, new MemoryCacheEntryOptions
                    {
                        AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10)
                    });
                }
            }
            return proveedor;
        }
    }
}
