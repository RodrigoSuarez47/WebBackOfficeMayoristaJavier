using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Newtonsoft.Json;
using System.Net.Http.Headers;
using WebMVC.Models;
using System.Threading.Tasks;
using DTOs;
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
                client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
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
                else
                {
                    string errorResponse = await respuesta.Content.ReadAsStringAsync().ConfigureAwait(false);
                    ViewBag.AlertIcon = "error";
                    ViewBag.AlertTitle = "Error";
                    ViewBag.Mensaje = $"Error al cargar los articulos: {errorResponse}";
                }
            }
            catch (Exception)
            {
                ViewBag.AlertIcon = "error";
                ViewBag.AlertTitle = "Error";
                ViewBag.Mensaje = "Ocurrió un error al cargar los articulos.";
            }

            return new List<ArticuloDTO>();
        }

        // GET: PedidosController
        public async Task<ActionResult> Index()
        {
            try
            {
                // Primero intenta obtener la lista de pedidos desde la caché
                if (!_memoryCache.TryGetValue(_cacheKey, out IEnumerable<PedidoDTO> pedidos))
                {
                    // Si no está en caché, realiza la llamada a la API para obtener los pedidos
                    pedidos = await GetPedidosFromApi();

                    // Almacena los pedidos en caché con una expiración de 10 minutos
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

        // Método para obtener los pedidos desde la API
        private async Task<IEnumerable<PedidoDTO>> GetPedidosFromApi()
        {
            try
            {
                var client = ConfigureClient();
                var response = await client.GetAsync(_urlApi);

                if (response.IsSuccessStatusCode)
                {
                    var jsonString = await response.Content.ReadAsStringAsync();
                    var pedidos = JsonConvert.DeserializeObject<IEnumerable<PedidoDTO>>(jsonString);
                    return pedidos;
                }
                else
                {
                    // Si la API responde con error, lanzamos una excepción con el mensaje de error
                    var errorMessage = await response.Content.ReadAsStringAsync();
                    throw new Exception($"Error al obtener pedidos: {errorMessage}");
                }
            }
            catch (Exception ex)
            {
                // Manejo de errores: Si algo falla en la llamada a la API
                throw new Exception("Hubo un error al intentar obtener los pedidos desde la API.", ex);
            }
        }

        // GET: PedidosController/Details/5
        public async Task<ActionResult> Details(int id)
        {
            try
            {
                // Llamada a la API para obtener los detalles del pedido
                var pedido = await GetPedidoFromApi(id);

                if (pedido == null)
                {
                    return NotFound();
                }

                return View(pedido);
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Hubo un error al intentar obtener los detalles del pedido.");
                return View();
            }
        }

        // Método para obtener un solo pedido desde la API
        private async Task<PedidoDTO> GetPedidoFromApi(int id)
        {
            try
            {
                var client = ConfigureClient();
                var response = await client.GetAsync($"{_urlApi}{id}");

                if (response.IsSuccessStatusCode)
                {
                    var jsonString = await response.Content.ReadAsStringAsync();
                    var pedido = JsonConvert.DeserializeObject<PedidoDTO>(jsonString);
                    return pedido;
                }
                else
                {
                    // Si la API responde con error, lanzamos una excepción
                    var errorMessage = await response.Content.ReadAsStringAsync();
                    throw new Exception($"Error al obtener el pedido: {errorMessage}");
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Hubo un error al intentar obtener el pedido desde la API.", ex);
            }
        }

        // GET: PedidosController/Create
        public async Task<IActionResult> Create()
        {
            List<ArticuloDTO> articulos = await GetArticulos();
            ViewBag.Articulos = articulos;
            return View();
        }


        // POST: PedidosController/Create
        [HttpPost]
        public async Task<ActionResult> Create(PedidoDTO pedido)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    // Llamada a la API para crear el pedido
                    var client = ConfigureClient();
                    var content = new StringContent(JsonConvert.SerializeObject(pedido), System.Text.Encoding.UTF8, "application/json");
                    var response = await client.PostAsync(_urlApi, content);

                    if (response.IsSuccessStatusCode)
                    {
                        // Si la creación es exitosa, redirige a la lista de pedidos
                        _memoryCache.Remove(_cacheKey); // Elimina la caché antigua
                        return RedirectToAction(nameof(Index));
                    }
                    else
                    {
                        // Si la API responde con un error, muestra el mensaje
                        var errorMessage = await response.Content.ReadAsStringAsync();
                        ModelState.AddModelError("", $"Error al crear el pedido: {errorMessage}");
                    }
                }

                return View(pedido);
            }
            catch (Exception ex)
            {
                // Manejo de excepciones
                ModelState.AddModelError("", "Hubo un error inesperado al intentar crear el pedido.");
                return View(pedido);
            }
        }

        // GET: PedidosController/Edit/5
        public async Task<ActionResult> Edit(int id)
        {
            try
            {
                var pedido = await GetPedidoFromApi(id);
                if (pedido == null)
                {
                    return NotFound();
                }

                return View(pedido);
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Hubo un error al intentar obtener el pedido para editar.");
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
                    var content = new StringContent(JsonConvert.SerializeObject(pedido), System.Text.Encoding.UTF8, "application/json");
                    var response = await client.PutAsync($"{_urlApi}{id}", content);

                    if (response.IsSuccessStatusCode)
                    {
                        // Si la actualización es exitosa, redirige a la lista de pedidos
                        _memoryCache.Remove(_cacheKey); // Elimina la caché antigua
                        return RedirectToAction(nameof(Index));
                    }
                    else
                    {
                        var errorMessage = await response.Content.ReadAsStringAsync();
                        ModelState.AddModelError("", $"Error al actualizar el pedido: {errorMessage}");
                    }
                }

                return View(pedido);
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Hubo un error inesperado al intentar actualizar el pedido.");
                return View(pedido);
            }
        }

        // GET: PedidosController/Delete/5
        public async Task<ActionResult> Delete(int id)
        {
            try
            {
                var pedido = await GetPedidoFromApi(id);
                if (pedido == null)
                {
                    return NotFound();
                }

                return View(pedido);
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Hubo un error al intentar obtener el pedido para eliminar.");
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
                    // Elimina la caché antigua
                    _memoryCache.Remove(_cacheKey);
                    return RedirectToAction(nameof(Index));
                }
                else
                {
                    var errorMessage = await response.Content.ReadAsStringAsync();
                    ModelState.AddModelError("", $"Error al eliminar el pedido: {errorMessage}");
                    return View();
                }
            }
            catch (Exception ex)
            {
                // Si ocurre un error inesperado, mostramos un mensaje genérico
                ModelState.AddModelError("", "Hubo un error al eliminar el pedido.");
                return View();
            }
        }
    }
}
