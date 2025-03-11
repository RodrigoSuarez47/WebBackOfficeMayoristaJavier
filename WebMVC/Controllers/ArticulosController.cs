using DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Caching.Memory;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
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
        UrlApi = config.GetValue<string>("UrlAPI") + "Article/"; // Asegúrate de que esta ruta sea correcta para tus artículos
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

                // Guarda en caché; si no se especifica otro tiempo, 30 minutos
                _memoryCache.Set(cacheKey, deserializedResponse, cacheDuration ?? TimeSpan.FromMinutes(30));
                return deserializedResponse;
            }
            else
            {
                ViewBag.Mensaje = "Error al procesar la solicitud";
            }
        }
        catch (Exception ex)
        {
            ViewBag.Mensaje = "Error al comunicarse con el servidor.";
            System.Diagnostics.Debug.WriteLine(ex.Message);
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
            return respuesta.IsSuccessStatusCode ? string.Empty : await respuesta.Content.ReadAsStringAsync().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(ex.Message);
            return "Error al comunicarse con el servidor.";
        }
    }

    // GET: ArticulosController
    public async Task<ActionResult> List()
    {
        // Se intenta cargar desde caché; si no, se consulta la API y se cachea por 10 minutos
        var articulos = await GetApiResponse<List<ArticuloDTO>>("", CacheKey, TimeSpan.FromMinutes(10)).ConfigureAwait(false);
        return View(articulos ?? new List<ArticuloDTO>());
    }

    // GET: ArticulosController/Details/5
    public async Task<ActionResult> Details(int id)
    {
        var articulo = await GetApiResponse<ArticuloDTO>($"{id}", $"Articulo_{id}", TimeSpan.FromMinutes(30)).ConfigureAwait(false);
        if (articulo != null)
        {
            return View(articulo);
        }
        ViewBag.Mensaje = "No se pudo obtener el detalle del artículo.";
        return View();
    }

    // GET: ArticulosController/Create
    public async Task<IActionResult> Create()
    {
        try
        {
            var client = ConfigureClient();
            var respuesta = await client.GetAsync(UrlApiProveedores).ConfigureAwait(false);

            if (respuesta.IsSuccessStatusCode)
            {
                string cuerpo = await respuesta.Content.ReadAsStringAsync().ConfigureAwait(false);
                var proveedores = JsonConvert.DeserializeObject<List<ProveedorDTO>>(cuerpo);
                _memoryCache.Set(CacheKeyProveedores, proveedores, TimeSpan.FromMinutes(30));
                ViewBag.Proveedores = new List<ProveedorDTO>(proveedores) ?? new List<ProveedorDTO>();
            }
            else
            {
                ViewBag.Proveedores = new List<ProveedorDTO>();
                ViewBag.Mensaje = "No se pudieron cargar los proveedores. Intente nuevamente.";
            }
        }
        catch (Exception)
        {
            ViewBag.Proveedores = new List<ProveedorDTO>();
            ViewBag.Mensaje = "Ocurrió un error al cargar los proveedores.";
        }
        return View(new ArticuloDTO());
    }



    // POST: ArticulosController/Create
    [HttpPost]
    public async Task<ActionResult> Create(ArticuloDTO articulo)
    {
        var errorMessage = await PostOrPutApiResponse("", HttpMethod.Post, articulo).ConfigureAwait(false);
        if (string.IsNullOrEmpty(errorMessage))
        {
            _memoryCache.Remove(CacheKey); // Invalida el caché de la lista de artículos
            return RedirectToAction(nameof(List));
        }
        ViewBag.Mensaje = "Error al crear el artículo: " + errorMessage;
        return RedirectToAction("Create", articulo);
    }

    // GET: ArticulosController/Edit/5
    public async Task<ActionResult> Edit(int id)
    {
        var articulos = await GetApiResponse<Collection<ArticuloDTO>>("?", CacheKey, TimeSpan.FromMinutes(30)).ConfigureAwait(false);
        var articulo = articulos?.FirstOrDefault(a => a.Id == id);
        if (articulo != null)
        {
            try
            {
                var client = ConfigureClient();
                var respuesta = await client.GetAsync(UrlApiProveedores).ConfigureAwait(false);

                if (respuesta.IsSuccessStatusCode)
                {
                    string cuerpo = await respuesta.Content.ReadAsStringAsync().ConfigureAwait(false);
                    var proveedores = JsonConvert.DeserializeObject<List<ProveedorDTO>>(cuerpo);
                    _memoryCache.Set(CacheKeyProveedores, proveedores, TimeSpan.FromMinutes(30));
                    ViewBag.Proveedores = new List<ProveedorDTO>(proveedores) ?? new List<ProveedorDTO>();
                }
                else
                {
                    ViewBag.Proveedores = new List<ProveedorDTO>();
                    ViewBag.Mensaje = "No se pudieron cargar los proveedores. Intente nuevamente.";
                }
            }
            catch (Exception)
            {
                ViewBag.Proveedores = new List<ProveedorDTO>();
                ViewBag.Mensaje = "Ocurrió un error al cargar los proveedores.";
            }
            return View(articulo);
        }
        // Si no se encuentra el artículo, mostramos un mensaje de error
        ViewBag.Mensaje = "No se pudo obtener el artículo para editar.";
        return RedirectToAction(nameof(List));
    }



    // POST: ArticulosController/Edit/5
    [HttpPost]
    public async Task<ActionResult> Edit(int id, ArticuloDTO articulo)
    {
        var errorMessage = await PostOrPutApiResponse($"{id}", HttpMethod.Put, articulo).ConfigureAwait(false);
        if (string.IsNullOrEmpty(errorMessage))
        {
            _memoryCache.Remove(CacheKey);
            _memoryCache.Remove($"Articulo_{id}");
            return RedirectToAction(nameof(List));
        }
        ViewBag.Mensaje = "Error al actualizar el artículo: " + errorMessage;
        return RedirectToAction("Edit", articulo);
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
                // Eliminación exitosa
                _memoryCache.Remove(CacheKey);
                _memoryCache.Remove($"Articulo_{id}");
                return RedirectToAction(nameof(List));
            }
            else
            {
                // Si la respuesta contiene un mensaje, significa que hubo un error
                ViewBag.Mensaje = "Error al eliminar el artículo: " + response;
                return View();
            }
        }
        catch (Exception ex)
        {
            ViewBag.Mensaje = "Ocurrió un error al intentar eliminar el artículo: " + ex.Message;
            return View();
        }
    }
}
