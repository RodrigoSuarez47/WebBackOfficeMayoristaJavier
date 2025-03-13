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
    private readonly string CacheKeyProveedores = "ListaProveedores";

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
                ViewBag.AlertIcon = "error";
                ViewBag.AlertTitle = "Error";
                ViewBag.Mensaje = $"Error: {errorResponse}";
            }
        }
        catch (Exception ex)
        {
            ViewBag.AlertIcon = "error";
            ViewBag.AlertTitle = "Error";
            ViewBag.Mensaje = $"Error al comunicarse con el servidor: {ex.Message}";
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
        if (_memoryCache.TryGetValue(CacheKeyProveedores, out List<ProveedorDTO> cachedProveedores))
        {
            return cachedProveedores;
        }

        try
        {
            var client = ConfigureClient();
            var respuesta = await client.GetAsync(UrlApiProveedores).ConfigureAwait(false);

            if (respuesta.IsSuccessStatusCode)
            {
                string cuerpo = await respuesta.Content.ReadAsStringAsync().ConfigureAwait(false);
                var proveedores = JsonConvert.DeserializeObject<List<ProveedorDTO>>(cuerpo);
                _memoryCache.Set(CacheKeyProveedores, proveedores, TimeSpan.FromMinutes(30));
                return proveedores ?? new List<ProveedorDTO>();
            }
            else
            {
                string errorResponse = await respuesta.Content.ReadAsStringAsync().ConfigureAwait(false);
                ViewBag.AlertIcon = "error";
                ViewBag.AlertTitle = "Error";
                ViewBag.Mensaje = $"Error al cargar los proveedores: {errorResponse}";
            }
        }
        catch (Exception)
        {
            ViewBag.AlertIcon = "error";
            ViewBag.AlertTitle = "Error";
            ViewBag.Mensaje = "Ocurrió un error al cargar los proveedores.";
        }

        return new List<ProveedorDTO>();
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
            ViewBag.AlertIcon = "error";
            ViewBag.AlertTitle = "Error";
            ViewBag.Mensaje = $"Error al cargar los artículos: {ex.Message}";
            ViewBag.Proveedores = new List<ProveedorDTO>(); 
            return View(new List<ArticuloDTO>());
        }
    }


    // GET: ArticulosController/Details/5
    public async Task<ActionResult> Details(int id)
    {
        try
        {
            var articulo = await GetApiResponse<ArticuloDTO>($"{id}", $"Articulo_{id}", TimeSpan.FromMinutes(30)).ConfigureAwait(false);
            if (articulo != null)
            {
                return View(articulo);
            }
            ViewBag.AlertIcon = "error";
            ViewBag.AlertTitle = "Error";
            ViewBag.Mensaje = "No se pudo obtener el detalle del artículo.";
            return View();
        }
        catch (Exception ex)
        {
            ViewBag.AlertIcon = "error";
            ViewBag.AlertTitle = "Error";
            ViewBag.Mensaje = $"Error al obtener el detalle del artículo: {ex.Message}";
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
        try
        {
            var errorMessage = await PostOrPutApiResponse("", HttpMethod.Post, articulo).ConfigureAwait(false);
            if (string.IsNullOrEmpty(errorMessage))
            {
                _memoryCache.Remove(CacheKey); // Invalida el caché de la lista de artículos
                return RedirectToAction(nameof(List));
            }
            ViewBag.AlertIcon = "error";
            ViewBag.AlertTitle = "Error";
            ViewBag.Mensaje = $"Error al crear el artículo: {errorMessage}";
            return View(articulo);
        }
        catch (Exception ex)
        {
            ViewBag.AlertIcon = "error";
            ViewBag.AlertTitle = "Error";
            ViewBag.Mensaje = $"Error al crear el artículo: {ex.Message}";
            return View(articulo);
        }
    }

    // GET: ArticulosController/Edit/5
    public async Task<ActionResult> Edit(int id)
    {
        try
        {
            var articulos = await GetApiResponse<Collection<ArticuloDTO>>("", CacheKey, TimeSpan.FromMinutes(30)).ConfigureAwait(false);
            var articulo = articulos?.FirstOrDefault(a => a.Id == id);

            if (articulo != null)
            {
                ViewBag.Proveedores = await GetProveedores();
                return View(articulo);
            }

            ViewBag.AlertIcon = "error";
            ViewBag.AlertTitle = "Error";
            ViewBag.Mensaje = "No se pudo obtener el artículo para editar.";
            return RedirectToAction(nameof(List));
        }
        catch (Exception ex)
        {
            ViewBag.AlertIcon = "error";
            ViewBag.AlertTitle = "Error";
            ViewBag.Mensaje = $"Error al cargar el artículo para editar: {ex.Message}";
            return RedirectToAction(nameof(List));
        }
    }


    // POST: ArticulosController/Edit/5
    [HttpPost]
    public async Task<ActionResult> Edit(int id, ArticuloDTO articulo)
    {
        try
        {
            var errorMessage = await PostOrPutApiResponse($"{id}", HttpMethod.Put, articulo).ConfigureAwait(false);
            if (string.IsNullOrEmpty(errorMessage))
            {
                _memoryCache.Remove(CacheKey);
                _memoryCache.Remove($"Articulo_{id}");
                return RedirectToAction(nameof(List));
            }
            ViewBag.AlertIcon = "error";
            ViewBag.AlertTitle = "Error";
            ViewBag.Mensaje = $"Error al actualizar el artículo: {errorMessage}";
            return View(articulo);
        }
        catch (Exception ex)
        {
            ViewBag.AlertIcon = "error";
            ViewBag.AlertTitle = "Error";
            ViewBag.Mensaje = $"Error al actualizar el artículo: {ex.Message}";
            return View(articulo);
        }
    }

    // POST: ArticulosController/Delete/5
    [HttpPost]
    public async Task<ActionResult> Delete(int id)
    {
        try
        {
            var response = await PostOrPutApiResponse($"{id}", HttpMethod.Delete).ConfigureAwait(false);

            if (string.IsNullOrEmpty(response))
            {
                _memoryCache.Remove(CacheKey);
                _memoryCache.Remove($"Articulo_{id}");
                return RedirectToAction(nameof(List));
            }
            else
            {
                ViewBag.AlertIcon = "error";
                ViewBag.AlertTitle = "Error";
                ViewBag.Mensaje = $"Error al eliminar el artículo: {response}";
                return View();
            }
        }
        catch (Exception ex)
        {
            ViewBag.AlertIcon = "error";
            ViewBag.AlertTitle = "Error";
            ViewBag.Mensaje = $"Error al intentar eliminar el artículo: {ex.Message}";
            return View();
        }
    }

    [HttpPost]
    public async Task<IActionResult> ModificarStock(int articleId, int newStock)
    {
        try
        {
            var errorMessage = await PostOrPutApiResponse($"UpdateStock/{articleId}", HttpMethod.Put, newStock).ConfigureAwait(false);

            if (string.IsNullOrEmpty(errorMessage))
            {
                _memoryCache.Remove(CacheKey);
                _memoryCache.Remove($"Articulo_{articleId}");
                ViewBag.AlertIcon = "success";
                ViewBag.AlertTitle = "Éxito";
                ViewBag.Mensaje = "El stock del artículo ha sido actualizado correctamente.";
                return RedirectToAction(nameof(List));
            }
            ViewBag.AlertIcon = "error";
            ViewBag.AlertTitle = "Error";
            ViewBag.Mensaje = $"Error al actualizar el stock: {errorMessage}";
            return View("List");
        }
        catch (Exception ex)
        {
            ViewBag.AlertIcon = "error";
            ViewBag.AlertTitle = "Error";
            ViewBag.Mensaje = $"Error al intentar actualizar el stock: {ex.Message}";
            return View("List");
        }
    }


    [HttpPost]
    public async Task<IActionResult> ModificarPrecios(int articleId, decimal purchasePrice, decimal salePrice, decimal unitSalePrice)
    {
        try
        {
            var articulo = await GetApiResponse<ArticuloDTO>($"{articleId}", $"{articleId}").ConfigureAwait(false);
            if (articulo == null)
            {
                ViewBag.AlertIcon = "error";
                ViewBag.AlertTitle = "Error";
                ViewBag.Mensaje = "Artículo no encontrado.";
                return RedirectToAction(nameof(List));
            }

            articulo.PurchasePrice = purchasePrice;
            articulo.SalePrice = salePrice;
            articulo.UnitSalePrice = unitSalePrice;

            var errorMessage = await PostOrPutApiResponse($"UpdatePrices/{articleId}", HttpMethod.Put, articulo).ConfigureAwait(false);

            if (string.IsNullOrEmpty(errorMessage))
            {
                _memoryCache.Remove(CacheKey);
                _memoryCache.Remove($"Articulo_{articleId}");

                ViewBag.AlertIcon = "success";
                ViewBag.AlertTitle = "Éxito";
                ViewBag.Mensaje = "Los precios del artículo han sido actualizados correctamente.";
                return RedirectToAction(nameof(List));
            }

            ViewBag.AlertIcon = "error";
            ViewBag.AlertTitle = "Error";
            ViewBag.Mensaje = $"Error al actualizar los precios: {errorMessage}";
            return RedirectToAction(nameof(List));
        }
        catch (Exception ex)
        {
            // Manejo de excepciones
            ViewBag.AlertIcon = "error";
            ViewBag.AlertTitle = "Error";
            ViewBag.Mensaje = $"Error al intentar actualizar los precios: {ex.Message}";
            return RedirectToAction(nameof(List));
        }
    }
    [HttpPost]
    public async Task<IActionResult> CambiarVisibilidad(int id)
    {
        // Obtener el artículo por id
        var articulo = await GetApiResponse<ArticuloDTO>($"{id}", $"{id}").ConfigureAwait(false);

        if (articulo == null)
        {
            ViewBag.AlertIcon = "error";
            ViewBag.AlertTitle = "Error";
            ViewBag.Mensaje = "Artículo no encontrado.";
            return RedirectToAction(nameof(List)); 
        }

        articulo.IsVisible = !articulo.IsVisible;

        var errorMessage = await PostOrPutApiResponse($"ToggleVisibility/{id}", HttpMethod.Put, articulo).ConfigureAwait(false);

        if (string.IsNullOrEmpty(errorMessage))
        {
            _memoryCache.Remove(CacheKey);
            _memoryCache.Remove($"Articulo_{id}");

            ViewBag.AlertIcon = "success";
            ViewBag.AlertTitle = "Éxito";
            ViewBag.Mensaje = articulo.IsVisible ? "Artículo ahora visible." : "Artículo ahora no visible.";

            return RedirectToAction(nameof(List));
        }
        else
        {
            ViewBag.AlertIcon = "error";
            ViewBag.AlertTitle = "Error";
            ViewBag.Mensaje = "No se pudo actualizar la visibilidad del artículo.";

            return View("List"); 
        }
    }
}