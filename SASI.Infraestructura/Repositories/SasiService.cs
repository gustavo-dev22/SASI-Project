using Microsoft.Extensions.Configuration;
using SASI.Dominio.DTO;
using SASI.Dominio.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;

namespace SASI.Infraestructura.Repositories
{
    public class SasiService : ISasiService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;

        public SasiService(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _configuration = configuration;
        }

        public async Task<AccesosSasiResponseDto> ObtenerAccesosUsuario(string userName, string password)
        {
            var baseUrl = _configuration["SasiApi:BaseUrl"]?.TrimEnd('/');
            var endpoint = $"{baseUrl}/SASI/api/Auth/login";

            var request = new { UserName = userName, Password = password };

            var response = await _httpClient.PostAsJsonAsync(endpoint, request);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                throw new Exception($"Error al obtener accesos desde SASI. Código: {response.StatusCode}. Detalles: {errorContent}");
            }

            var data = await response.Content.ReadFromJsonAsync<AccesosSasiResponseDto>();
            return data!;
        }

        public async Task<AccesosSasiResponseDto> ObtenerAccesosUsuarioConToken(string userName, string token)
        {
            var baseUrl = _configuration["SasiApi:BaseUrl"]?.TrimEnd('/');
            var endpoint = $"{baseUrl}/SASI/api/Auth/accesos-usuario/{userName}";

            using var requestMessage = new HttpRequestMessage(HttpMethod.Get, endpoint);
            requestMessage.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            var response = await _httpClient.SendAsync(requestMessage);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                throw new Exception($"Error al obtener accesos desde SASI. Código: {response.StatusCode}. Detalles: {errorContent}");
            }

            var data = await response.Content.ReadFromJsonAsync<AccesosSasiResponseDto>();
            return data!;
        }
    }
}
