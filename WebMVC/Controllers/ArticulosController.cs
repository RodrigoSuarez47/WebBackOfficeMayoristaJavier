using DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Templates.Blazor;
using Newtonsoft.Json;
using System;
using System.Collections.ObjectModel;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

public class ArticulosController : Controller
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IMemoryCache _memoryCache;
    private readonly string UrlApi;
    private readonly string CacheKey = "ListaArticulos";

    public ArticulosController(IHttpClientFactory httpClientFactory, IMemoryCache memoryCache, IConfiguration config)
    {
        _httpClientFactory = httpClientFactory;
        _memoryCache = memoryCache;
        // Asegúrate de que la URL incluya la barra final
        UrlApi = config.GetValue<string>("UrlAPI") + "Article/";
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
            var client = _httpClientFactory.CreateClient();
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
            var client = _httpClientFactory.CreateClient();
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
        var articulos = await GetApiResponse<Collection<ArticuloDTO>>("", CacheKey, TimeSpan.FromMinutes(10)).ConfigureAwait(false);
        return View(articulos ?? new Collection<ArticuloDTO>());
    }

    // GET: ArticulosController/Details/5
    public async Task<ActionResult> Details(int id)
    {
        var articulo = await GetApiResponse<ArticuloDTO>(id.ToString(), $"Articulo_{id}", TimeSpan.FromMinutes(30)).ConfigureAwait(false);
        if (articulo != null)
        {
            return View(articulo);
        }
        ViewBag.Mensaje = "No se pudo obtener el detalle del artículo.";
        return View();
    }

    // GET: ArticulosController/Create
    public ActionResult Create()
    {
        return View(); // Devuelve la vista para crear un artículo
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
        return View(articulo); // Vuelve a la vista con los datos ingresados, para que el usuario corrija errores
    }


    // GET: ArticulosController/Edit/5
    public async Task<ActionResult> Edit(int id)
    {
        var articulo = await GetApiResponse<ArticuloDTO>(id.ToString(), $"Articulo_{id}", TimeSpan.FromMinutes(30)).ConfigureAwait(false);
        if (articulo != null)
        {
            return View(articulo);
        }
        ViewBag.Mensaje = "No se pudo obtener el artículo para editar.";
        return RedirectToAction(nameof(List));
    }

    // POST: ArticulosController/Edit/5
    [HttpPost]
    public async Task<ActionResult> Edit(int id, ArticuloDTO articulo)
    {
        var errorMessage = await PostOrPutApiResponse(id.ToString(), HttpMethod.Put, articulo).ConfigureAwait(false);
        if (string.IsNullOrEmpty(errorMessage))
        {
            _memoryCache.Remove(CacheKey);
            _memoryCache.Remove($"Articulo_{id}");
            return RedirectToAction(nameof(List));
        }
        ViewBag.Mensaje = "Error al actualizar el artículo: " + errorMessage;
        return View(articulo);
    }
    // GET: ArticulosController/Delete/5
    public async Task<ActionResult> Delete(int id)
    {
        var articulo = await GetApiResponse<ArticuloDTO>(id.ToString(), $"Articulo_{id}", TimeSpan.FromMinutes(30)).ConfigureAwait(false);
        if (articulo != null)
        {
            return View(articulo);
        }
        ViewBag.Mensaje = "No se pudo obtener el artículo para eliminar.";
        return RedirectToAction(nameof(List));
    }

    // POST: ArticulosController/DeleteConfirmado/5
    [HttpPost]
    public async Task<ActionResult> DeleteConfirmado(int id)
    {
        try
        {
            var response = await PostOrPutApiResponse("DeleteArticle/" + id.ToString(), HttpMethod.Delete).ConfigureAwait(false);

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
