using DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Newtonsoft.Json;
using System.Text;
using WebMVC.Filtros;

namespace WebMVC.Controllers
{
    [UsuarioLogueado]
    public class PedidosController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IMemoryCache _memoryCache;
        private readonly string _urlApi;
        private readonly string _urlApiArticulos;
        private readonly string _cacheKey = "ListaPedidos";
        private readonly string _cacheKeyArticulos = "ListaArticulos";

        public PedidosController(IHttpClientFactory httpClientFactory, IMemoryCache memoryCache, IConfiguration config)
        {
            _httpClientFactory = httpClientFactory;
            _memoryCache = memoryCache;
            _urlApi = config.GetValue<string>("UrlAPI") + "Orders/";
            _urlApiArticulos = config.GetValue<string>("UrlAPI") + "Article/";
        }

        private HttpClient ConfigureClient()
        {
            var client = _httpClientFactory.CreateClient();
            var token = HttpContext.Session.GetString("Token");

            if (!string.IsNullOrEmpty(token))
            {
                client.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            }

            return client;
        }

        private async Task<List<ArticuloDTO>> GetArticulos()
        {
            if (_memoryCache.TryGetValue(_cacheKeyArticulos, out List<ArticuloDTO> cachedArticulos))
            {
                return cachedArticulos;
            }

            try
            {
                var client = ConfigureClient();
                var respuesta = await client.GetAsync(_urlApiArticulos).ConfigureAwait(false);

                if (respuesta.IsSuccessStatusCode)
                {
                    string cuerpo = await respuesta.Content.ReadAsStringAsync().ConfigureAwait(false);
                    var articulos = JsonConvert.DeserializeObject<List<ArticuloDTO>>(cuerpo);
                    _memoryCache.Set(_cacheKeyArticulos, articulos, TimeSpan.FromMinutes(30));
                    return articulos ?? new List<ArticuloDTO>();
                }

                string errorResponse = await respuesta.Content.ReadAsStringAsync().ConfigureAwait(false);
                ViewBag.AlertIcon = "error";
                ViewBag.AlertTitle = "Error";
                ViewBag.Mensaje = $"Error al cargar los articulos: {errorResponse}";
                return new List<ArticuloDTO>();
            }
            catch (Exception)
            {
                ViewBag.AlertIcon = "error";
                ViewBag.AlertTitle = "Error";
                ViewBag.Mensaje = "Ocurrió un error al cargar los articulos.";
                return new List<ArticuloDTO>();
            }
        }

        // GET: PedidosController
        public async Task<ActionResult> Index()
        {
            try
            {
                if (!_memoryCache.TryGetValue(_cacheKey, out IEnumerable<PedidoDTO> pedidos))
                {
                    pedidos = await GetPedidosFromApi();
                    var cacheExpirationOptions = new MemoryCacheEntryOptions()
                        .SetAbsoluteExpiration(TimeSpan.FromMinutes(10));
                    _memoryCache.Set(_cacheKey, pedidos, cacheExpirationOptions);
                }

                return View(pedidos);
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Hubo un error al intentar obtener los pedidos.");
                return View(new List<PedidoDTO>());
            }
        }

        private async Task<IEnumerable<PedidoDTO>> GetPedidosFromApi()
        {
            try
            {
                var client = ConfigureClient();
                var response = await client.GetAsync(_urlApi);

                if (response.IsSuccessStatusCode)
                {
                    var jsonString = await response.Content.ReadAsStringAsync();
                    return JsonConvert.DeserializeObject<IEnumerable<PedidoDTO>>(jsonString);
                }

                var errorMessage = await response.Content.ReadAsStringAsync();
                throw new Exception($"Error al obtener pedidos: {errorMessage}");
            }
            catch (Exception ex)
            {
                throw new Exception("Hubo un error al intentar obtener los pedidos desde la API.", ex);
            }
        }

        // GET: PedidosController/Details/5
        public async Task<ActionResult> Details(int id)
        {
            try
            {
                var pedido = await GetPedidoFromApi(id);
                return pedido == null ? NotFound() : View(pedido);
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Hubo un error al obtener los detalles del pedido.");
                return View();
            }
        }

        private async Task<PedidoDTO> GetPedidoFromApi(int id)
        {
            try
            {
                var client = ConfigureClient();
                var response = await client.GetAsync($"{_urlApi}{id}");

                if (response.IsSuccessStatusCode)
                {
                    var jsonString = await response.Content.ReadAsStringAsync();
                    return JsonConvert.DeserializeObject<PedidoDTO>(jsonString);
                }

                var errorMessage = await response.Content.ReadAsStringAsync();
                throw new Exception($"Error al obtener el pedido: {errorMessage}");
            }
            catch (Exception ex)
            {
                throw new Exception("Hubo un error al obtener el pedido desde la API.", ex);
            }
        }

        // GET: PedidosController/Create
        public async Task<IActionResult> Create()
        {
            ViewBag.Articulos = await GetArticulos();
            return View(new PedidoDTO());
        }

        // POST: PedidosController/Create
        [HttpPost]
        public async Task<ActionResult> Create(PedidoDTO pedido)
        {
            var articulos = await GetArticulos();
            ViewBag.Articulos = articulos;

            try
            {
                if (pedido.Lines == null || !pedido.Lines.Any())
                {
                    ModelState.AddModelError("", "Debe agregar al menos un artículo al pedido.");
                    return View(pedido);
                }

                // Asignar los artículos completos a las líneas
                foreach (var linea in pedido.Lines)
                {
                    if (linea.ArticleId == 0)
                    {
                        ModelState.AddModelError("", $"Artículo ID {linea.ArticleId} no encontrado");
                        return View(pedido);
                    }
                }

                var client = ConfigureClient();
                var content = new StringContent(
                    JsonConvert.SerializeObject(pedido),
                    Encoding.UTF8,
                    "application/json");

                var response = await client.PostAsync(_urlApi, content);

                if (response.IsSuccessStatusCode)
                {
                    _memoryCache.Remove(_cacheKey);
                    _memoryCache.Remove(_cacheKeyArticulos); // Invalida la caché de artículos
                    return RedirectToAction(nameof(Index));
                }

                var errorMessage = await response.Content.ReadAsStringAsync();
                ModelState.AddModelError("", $"Error al crear el pedido: {errorMessage}");
                return View(pedido);
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Hubo un error inesperado al crear el pedido.");
                return View(pedido);
            }
        }

        // GET: PedidosController/Edit/5
        public async Task<ActionResult> Edit(int id)
        {
            try
            {
                var pedido = await GetPedidoFromApi(id);
                return pedido == null ? NotFound() : View(pedido);
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Error al obtener el pedido para editar.");
                return View();
            }
        }

        // POST: PedidosController/Edit/5
        [HttpPost]
        public async Task<ActionResult> Edit(int id, PedidoDTO pedido)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    var client = ConfigureClient();
                    var content = new StringContent(
                        JsonConvert.SerializeObject(pedido),
                        Encoding.UTF8,
                        "application/json");

                    var response = await client.PutAsync($"{_urlApi}{id}", content);

                    if (response.IsSuccessStatusCode)
                    {
                        _memoryCache.Remove(_cacheKey);
                        return RedirectToAction(nameof(Index));
                    }

                    var errorMessage = await response.Content.ReadAsStringAsync();
                    ModelState.AddModelError("", $"Error al actualizar: {errorMessage}");
                }
                return View(pedido);
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Error inesperado al actualizar.");
                return View(pedido);
            }
        }

        // GET: PedidosController/Delete/5
        public async Task<ActionResult> Delete(int id)
        {
            try
            {
                var pedido = await GetPedidoFromApi(id);
                return pedido == null ? NotFound() : View(pedido);
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Error al obtener pedido para eliminar.");
                return View();
            }
        }

        // POST: PedidosController/Delete/5
        [HttpPost, ActionName("Delete")]
        public async Task<ActionResult> DeleteConfirmed(int id)
        {
            try
            {
                var client = ConfigureClient();
                var response = await client.DeleteAsync($"{_urlApi}{id}");

                if (response.IsSuccessStatusCode)
                {
                    _memoryCache.Remove(_cacheKey);
                    return RedirectToAction(nameof(Index));
                }

                var errorMessage = await response.Content.ReadAsStringAsync();
                ModelState.AddModelError("", $"Error al eliminar: {errorMessage}");
                return View();
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Error inesperado al eliminar.");
                return View();
            }
        }
    }
}
