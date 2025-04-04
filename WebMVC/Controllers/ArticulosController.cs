using DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Newtonsoft.Json;
using System.Collections.ObjectModel;
using System.Net.Http.Headers;
using System.Text;
using WebMVC.Filtros;

[UsuarioLogueado]
public class ArticulosController : Controller
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IMemoryCache _memoryCache;
    private readonly string UrlApi;
    private readonly string CacheKey = "ListaArticulos";
    private readonly string UrlApiProveedores;

    public ArticulosController(IHttpClientFactory httpClientFactory, IMemoryCache memoryCache, IConfiguration config)
    {
        _httpClientFactory = httpClientFactory;
        _memoryCache = memoryCache;
        UrlApi = config.GetValue<string>("UrlAPI") + "Article/";
        UrlApiProveedores = config.GetValue<string>("UrlAPI") + "Suppliers/";
    }

    private HttpClient ConfigureClient()
    {
        var client = _httpClientFactory.CreateClient();
        var token = HttpContext.Session.GetString("Token");
        if (!string.IsNullOrEmpty(token))
        {
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }
        return client;
    }

    private async Task<T> GetApiResponse<T>(string endpoint, string cacheKey = null, TimeSpan? cacheDuration = null)
    {
        cacheKey ??= endpoint;

        if (_memoryCache.TryGetValue(cacheKey, out T cachedResponse))
        {
            return cachedResponse;
        }

        try
        {
            var client = ConfigureClient();
            var respuesta = await client.GetAsync(UrlApi + endpoint).ConfigureAwait(false);

            if (respuesta.IsSuccessStatusCode)
            {
                string cuerpo = await respuesta.Content.ReadAsStringAsync().ConfigureAwait(false);
                var deserializedResponse = JsonConvert.DeserializeObject<T>(cuerpo);

                _memoryCache.Set(cacheKey, deserializedResponse, cacheDuration ?? TimeSpan.FromMinutes(30));
                return deserializedResponse;
            }
            else
            {
                string errorResponse = await respuesta.Content.ReadAsStringAsync().ConfigureAwait(false);
                SetAlert("error", "Error", $"Error: {errorResponse}");
            }
        }
        catch (Exception ex)
        {
            SetAlert("error", "Error", $"Error al comunicarse con el servidor: {ex.Message}");
        }

        return default;
    }

    private async Task<string> PostOrPutApiResponse(string endpoint, HttpMethod method, object data = null)
    {
        try
        {
            var client = ConfigureClient();
            var requestMessage = new HttpRequestMessage(method, UrlApi + endpoint)
            {
                Content = data != null ? new StringContent(JsonConvert.SerializeObject(data), Encoding.UTF8, "application/json") : null
            };

            var respuesta = await client.SendAsync(requestMessage).ConfigureAwait(false);

            if (!respuesta.IsSuccessStatusCode)
            {
                string errorResponse = await respuesta.Content.ReadAsStringAsync().ConfigureAwait(false);
                return $"Error: {errorResponse}";
            }

            return string.Empty;
        }
        catch (Exception ex)
        {
            return $"Error al comunicarse con el servidor: {ex.Message}";
        }
    }

    private async Task<List<ProveedorDTO>> GetProveedores()
    {
        try
        {
            var client = ConfigureClient();
            var respuesta = await client.GetAsync(UrlApiProveedores).ConfigureAwait(false);

            if (respuesta.IsSuccessStatusCode)
            {
                string cuerpo = await respuesta.Content.ReadAsStringAsync().ConfigureAwait(false);
                var proveedores = JsonConvert.DeserializeObject<List<ProveedorDTO>>(cuerpo);
                return proveedores ?? new List<ProveedorDTO>();
            }
            else
            {
                string errorResponse = await respuesta.Content.ReadAsStringAsync().ConfigureAwait(false);
                SetAlert("error", "Error", $"Error al cargar los proveedores: {errorResponse}");
            }
        }
        catch (Exception)
        {
            SetAlert("error", "Error", "Ocurrió un error al cargar los proveedores.");
        }

        return new List<ProveedorDTO>();
    }

    private void SetAlert(string icon, string title, string message)
    {
        TempData["AlertIcon"] = icon;
        TempData["AlertTitle"] = title;
        TempData["Mensaje"] = message;
    }

    // GET: ArticulosController
    public async Task<ActionResult> List()
    {
        try
        {
            var articulos = await GetApiResponse<List<ArticuloDTO>>("", CacheKey, TimeSpan.FromMinutes(10)).ConfigureAwait(false);
            ViewBag.Proveedores = await GetProveedores();
            return View(articulos ?? new List<ArticuloDTO>());
        }
        catch (Exception ex)
        {
            SetAlert("error", "Error", $"Error al cargar los artículos: {ex.Message}");
            ViewBag.Proveedores = new List<ProveedorDTO>();
            return View(new List<ArticuloDTO>());
        }
    }

    // GET: ArticulosController/Details/5
    public async Task<ActionResult> Details(int id)
    {
        ViewBag.Proveedores = await GetProveedores();
        try
        {
            var articulo = await GetApiResponse<ArticuloDTO>($"{id}", $"Articulo_{id}", TimeSpan.FromMinutes(30)).ConfigureAwait(false);
            if (articulo != null)
            {
                return View(articulo);
            }
            SetAlert("error", "Error", "No se pudo obtener el detalle del artículo.");
            return View();
        }
        catch (Exception ex)
        {
            SetAlert("error", "Error", $"Error al obtener el detalle del artículo: {ex.Message}");
            return View();
        }
    }

    // GET: ArticulosController/Create
    public async Task<IActionResult> Create()
    {
        ViewBag.Proveedores = await GetProveedores();
        return View(new ArticuloDTO());
    }

    // POST: ArticulosController/Create
    [HttpPost]
    public async Task<ActionResult> Create(ArticuloDTO articulo)
    {
        ViewBag.Proveedores = await GetProveedores();
        try
        {
            var errorMessage = await PostOrPutApiResponse("", HttpMethod.Post, articulo).ConfigureAwait(false);
            if (string.IsNullOrEmpty(errorMessage))
            {
                SetAlert("success", "Éxito", "El artículo ha sido agregado correctamente.");
                _memoryCache.Remove(CacheKey);
                var articulosActualizados = await GetApiResponse<List<ArticuloDTO>>("", CacheKey, TimeSpan.FromMinutes(10)).ConfigureAwait(false);
                return View("List", articulosActualizados);
            }
            SetAlert("error", "Error", $"Error al crear el artículo: {errorMessage}");
            return View(articulo);
        }
        catch (Exception ex)
        {
            SetAlert("error", "Error", $"Error al crear el artículo: {ex.Message}");
            return View(articulo);
        }
    }

    // GET: ArticulosController/Edit/5
    public async Task<ActionResult> Edit(int id)
    {
        var articulos = await GetApiResponse<Collection<ArticuloDTO>>("", CacheKey, TimeSpan.FromMinutes(30)).ConfigureAwait(false);
        ViewBag.Proveedores = await GetProveedores();
        try
        {
            var articulo = articulos?.FirstOrDefault(a => a.Id == id);

            if (articulo != null)
            {
                return View(articulo);
            }

            SetAlert("error", "Error", "No se pudo obtener el artículo para editar.");
            return View("List", articulos);
        }
        catch (Exception ex)
        {
            SetAlert("error", "Error", $"Error al cargar el artículo para editar: {ex.Message}");
            return View("List", articulos);
        }
    }

    // POST: ArticulosController/Edit/5
    [HttpPost]
    public async Task<ActionResult> Edit(int id, ArticuloDTO articulo)
    {
        ViewBag.Proveedores = await GetProveedores();
        try
        {
            var errorMessage = await PostOrPutApiResponse($"{id}", HttpMethod.Put, articulo).ConfigureAwait(false);
            if (string.IsNullOrEmpty(errorMessage))
            {
                _memoryCache.Remove(CacheKey);
                _memoryCache.Remove($"Articulo_{id}");
                _memoryCache.Remove($"FilterBySupplier_{articulo.SupplierId}"); // Invalida la caché del filtro por proveedor
                SetAlert("success", "Éxito", "El artículo ha sido actualizado correctamente.");
                var articulosActualizados = await GetApiResponse<List<ArticuloDTO>>("", CacheKey, TimeSpan.FromMinutes(10)).ConfigureAwait(false);
                return View("List", articulosActualizados);
            }
            SetAlert("error", "Error", $"Error al actualizar el artículo: {errorMessage}");
            return View(articulo);
        }
        catch (Exception ex)
        {
            SetAlert("error", "Error", $"Error al actualizar el artículo: {ex.Message}");
            return View(articulo);
        }
    }

    // POST: ArticulosController/Delete/5
    [HttpPost]
    public async Task<ActionResult> Delete(int id)
    {
        var articulos = await GetApiResponse<List<ArticuloDTO>>("", CacheKey, TimeSpan.FromMinutes(10)).ConfigureAwait(false);
        ViewBag.Proveedores = await GetProveedores();
        try
        {
            var response = await PostOrPutApiResponse($"{id}", HttpMethod.Delete).ConfigureAwait(false);

            if (string.IsNullOrEmpty(response))
            {
                _memoryCache.Remove(CacheKey);
                _memoryCache.Remove($"Articulo_{id}");
                ArticuloDTO articuloEliminado = articulos.Find(a => a.Id == id);
                _memoryCache.Remove($"FilterBySupplier_{articuloEliminado.SupplierId}"); // Invalida la caché del filtro por proveedor
                SetAlert("success", "Éxito", "El artículo ha sido eliminado.");
                var articulosActualizados = await GetApiResponse<List<ArticuloDTO>>("", CacheKey, TimeSpan.FromMinutes(10)).ConfigureAwait(false);
                return View("List", articulosActualizados);
            }
            else
            {
                SetAlert("error", "Error", $"Error al eliminar el artículo: {response}");
                return View("List", articulos);
            }
        }
        catch (Exception ex)
        {
            SetAlert("error", "Error", $"Error al eliminar el artículo: {ex.Message}");
            return View("List", articulos);
        }
    }

    [HttpPost]
    public async Task<IActionResult> ModificarPrecios(int articleId, decimal purchasePrice, decimal salePrice, decimal unitSalePrice)
    {
        var articulos = await GetApiResponse<List<ArticuloDTO>>("", CacheKey, TimeSpan.FromMinutes(10)).ConfigureAwait(false);
        ViewBag.Proveedores = await GetProveedores();
        try
        {
            var articulo = await GetApiResponse<ArticuloDTO>($"{articleId}", $"{articleId}").ConfigureAwait(false);
            if (articulo == null)
            {
                TempData["AlertIcon"] = "error";
                TempData["AlertTitle"] = "Error";
                TempData["Mensaje"] = "Artículo no encontrado.";
                return View("List", articulos);
            }

            articulo.PurchasePrice = purchasePrice;
            articulo.SalePrice = salePrice;
            articulo.UnitSalePrice = unitSalePrice;

            var errorMessage = await PostOrPutApiResponse($"UpdatePrices/{articleId}", HttpMethod.Put, articulo).ConfigureAwait(false);

            if (string.IsNullOrEmpty(errorMessage))
            {
                _memoryCache.Remove(CacheKey);
                _memoryCache.Remove($"Articulo_{articleId}");
                _memoryCache.Remove($"FilterBySupplier_{articulo.SupplierId}"); // Invalida la caché del filtro por proveedor

                TempData["AlertIcon"] = "success";
                TempData["AlertTitle"] = "Éxito";
                TempData["Mensaje"] = "Los precios del artículo han sido actualizados correctamente.";
                var articulosActualizados = await GetApiResponse<List<ArticuloDTO>>("", CacheKey, TimeSpan.FromMinutes(10)).ConfigureAwait(false);
                return View("List", articulosActualizados);
            }

            TempData["AlertIcon"] = "error";
            TempData["AlertTitle"] = "Error";
            TempData["Mensaje"] = $"Error al actualizar los precios: {errorMessage}";
            return View("List", articulos);
        }
        catch (Exception ex)
        {
            TempData["AlertIcon"] = "error";
            TempData["AlertTitle"] = "Error";
            TempData["Mensaje"] = $"Error al intentar actualizar los precios: {ex.Message}";
            return View("List", articulos);
        }
    }

    [HttpPost]
    public async Task<IActionResult> ModificarStock(int articleId, int newStock)
    {
        var articulos = await GetApiResponse<List<ArticuloDTO>>("", CacheKey, TimeSpan.FromMinutes(10)).ConfigureAwait(false);
        ViewBag.Proveedores = await GetProveedores();
        try
        {
            var errorMessage = await PostOrPutApiResponse($"UpdateStock/{articleId}", HttpMethod.Put, newStock).ConfigureAwait(false);

            if (string.IsNullOrEmpty(errorMessage))
            {
                _memoryCache.Remove(CacheKey);
                _memoryCache.Remove($"Articulo_{articleId}");
                ArticuloDTO articuloModificado = articulos.Find(a => a.Id == articleId);
                _memoryCache.Remove($"FilterBySupplier_{articuloModificado.SupplierId}"); // Invalida la caché del filtro por proveedor

                TempData["AlertIcon"] = "success";
                TempData["AlertTitle"] = "Éxito";
                TempData["Mensaje"] = "El stock del artículo ha sido actualizado correctamente.";
                var articulosActualizados = await GetApiResponse<List<ArticuloDTO>>("", CacheKey, TimeSpan.FromMinutes(10)).ConfigureAwait(false);
                return View("List", articulosActualizados);
            }

            TempData["AlertIcon"] = "error";
            TempData["AlertTitle"] = "Error";
            TempData["Mensaje"] = $"Error al actualizar el stock: {errorMessage}";
            return View("List", articulos);
        }
        catch (Exception ex)
        {
            TempData["AlertIcon"] = "error";
            TempData["AlertTitle"] = "Error";
            TempData["Mensaje"] = $"Error al intentar actualizar el stock: {ex.Message}";
            return View("List", articulos);
        }
    }

    [HttpPost]
    public async Task<IActionResult> CambiarVisibilidad(int id)
    {
        var articulos = await GetApiResponse<List<ArticuloDTO>>("", CacheKey, TimeSpan.FromMinutes(10)).ConfigureAwait(false);
        // Obtener el artículo por id
        var articulo = await GetApiResponse<ArticuloDTO>($"{id}", $"{id}").ConfigureAwait(false);
        ViewBag.Proveedores = await GetProveedores();
        if (articulo == null)
        {
            TempData["AlertIcon"] = "error";
            TempData["AlertTitle"] = "Error";
            TempData["Mensaje"] = "Artículo no encontrado.";
            return View("List", articulos);
        }

        articulo.IsVisible = !articulo.IsVisible;

        var errorMessage = await PostOrPutApiResponse($"ToggleVisibility/{id}", HttpMethod.Put, articulo).ConfigureAwait(false);

        if (string.IsNullOrEmpty(errorMessage))
        {
            _memoryCache.Remove(CacheKey);
            _memoryCache.Remove($"Articulo_{id}");
            _memoryCache.Remove($"FilterBySupplier_{articulo.SupplierId}"); // Invalida la caché del filtro por proveedor

            TempData["AlertIcon"] = "success";
            TempData["AlertTitle"] = "Éxito";
            TempData["Mensaje"] = articulo.IsVisible ? "Artículo ahora visible." : "Artículo ahora NO visible.";
            var articulosActualizados = await GetApiResponse<List<ArticuloDTO>>("", CacheKey, TimeSpan.FromMinutes(10)).ConfigureAwait(false);
            return View("List", articulosActualizados);
        }
        else
        {
            TempData["AlertIcon"] = "error";
            TempData["AlertTitle"] = "Error";
            TempData["Mensaje"] = "No se pudo actualizar la visibilidad del artículo.";

            return View("List", articulos);
        }
    }

    public async Task<IActionResult> FiltrarArticulosPorProveedor(int proveedorId)
    {
        ViewBag.Proveedores = await GetProveedores();
        if (proveedorId == 0)
        {
            return RedirectToAction("List");
        }
        try
        {
            var cacheKey = $"FilterBySupplier_{proveedorId}";
            // No usar la caché para la llamada de filtrado
            var articuloFiltradoss = await GetApiResponse<List<ArticuloDTO>>($"FilterBySupplier/{proveedorId}", cacheKey, TimeSpan.FromSeconds(1)).ConfigureAwait(false);
            ViewBag.ProveedorSeleccionado = proveedorId;
            return View("List", articuloFiltradoss ?? new List<ArticuloDTO>());
        }
        catch (Exception ex)
        {
            TempData["AlertIcon"] = "error";
            TempData["AlertTitle"] = "Error";
            TempData["Mensaje"] = $"Error al filtrar los artículos por proveedor: {ex.Message}";
            return View("List", new List<ArticuloDTO>());
        }
    }
}
