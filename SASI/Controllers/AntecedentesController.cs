using Microsoft.AspNetCore.Mvc;
using SASI.Requests;
using SASI.Rest;
using System.Text.Json;
using System.Text;

namespace SASI.Controllers
{
    public class AntecedentesController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public AntecedentesController(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Index(string dni)
        {
            var client = _httpClientFactory.CreateClient();

            var request = new ConsultaAntecedentesRequest
            {
                clienteUsuario = "PIDE_AP_MINJUS",
                clienteClave = "M1NJU55",
                clienteSistema = "SISTEMA DE CONSULTAS",
                clienteIp = "",
                clienteMac = "E4-54-E8-92-0F-F9",
                nroDocUserClieFin = "12345678", // quien realiza la consulta
                nroDoc = dni
            };

            var json = JsonSerializer.Serialize(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await client.PostAsync("https://ws4.pide.gob.pe/Rest/PNP/APolicialPerNumDoc?out=json", content);

            if (!response.IsSuccessStatusCode)
            {
                ViewBag.Error = "Error en la consulta.";
                return View();
            }

            var resultJson = await response.Content.ReadAsStringAsync();
            var resultado = JsonSerializer.Deserialize<ConsultaAntecedentesResponse>(resultJson, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            return View(resultado);
        }
    }
}
