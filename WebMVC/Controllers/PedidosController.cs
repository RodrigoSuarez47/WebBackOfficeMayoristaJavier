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
        private readonly string _cacheKey = "ListaPedidos";

        public PedidosController(IHttpClientFactory httpClientFactory, IMemoryCache memoryCache, IConfiguration config)
        {
            _httpClientFactory = httpClientFactory;
            _memoryCache = memoryCache;
            _urlApi = config.GetValue<string>("UrlAPI") + "Orders/"; // Asegúrate que la ruta esté correcta
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

        // GET: PedidosController
        public async Task<ActionResult> Index()
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
                    // Manejo de errores: Si la API responde con error
                    throw new Exception($"Error al obtener pedidos: {response.ReasonPhrase}");
                }
            }
            catch (Exception ex)
            {
                // Manejo de excepciones: Log y re-lanzar error
                throw new Exception("Hubo un error al intentar obtener los pedidos desde la API.", ex);
            }
        }

        // GET: PedidosController/Details/5
        public async Task<ActionResult> Details(int id)
        {
            // Llamada a la API para obtener los detalles del pedido
            var pedido = await GetPedidoFromApi(id);

            if (pedido == null)
            {
                return NotFound();
            }

            return View(pedido);
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
                    // Manejo de errores: Si la API responde con error
                    throw new Exception($"Error al obtener el pedido: {response.ReasonPhrase}");
                }
            }
            catch (Exception ex)
            {
                // Manejo de excepciones
                throw new Exception("Hubo un error al intentar obtener el pedido desde la API.", ex);
            }
        }

        // GET: PedidosController/Create
        public ActionResult Create()
        {
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
                        throw new Exception($"Error al crear el pedido: {response.ReasonPhrase}");
                    }
                }

                return View(pedido);
            }
            catch (Exception ex)
            {
                // Manejo de excepciones: muestra el error en la vista
                ModelState.AddModelError("", ex.Message);
                return View(pedido);
            }
        }

        // GET: PedidosController/Edit/5
        public async Task<ActionResult> Edit(int id)
        {
            var pedido = await GetPedidoFromApi(id);
            if (pedido == null)
            {
                return NotFound();
            }

            return View(pedido);
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
                        throw new Exception($"Error al actualizar el pedido: {response.ReasonPhrase}");
                    }
                }

                return View(pedido);
            }
            catch (Exception ex)
            {
                // Manejo de excepciones
                ModelState.AddModelError("", ex.Message);
                return View(pedido);
            }
        }

        // GET: PedidosController/Delete/5
        public async Task<ActionResult> Delete(int id)
        {
            var pedido = await GetPedidoFromApi(id);
            if (pedido == null)
            {
                return NotFound();
            }

            return View(pedido);
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
                    throw new Exception($"Error al eliminar el pedido: {response.ReasonPhrase}");
                }
            }
            catch (Exception ex)
            {
                // Manejo de excepciones
                ModelState.AddModelError("", ex.Message);
                return View();
            }
        }
    }
}

