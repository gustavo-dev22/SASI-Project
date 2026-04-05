using Microsoft.AspNetCore.Http;
using SASI.Dominio.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SASI.Infraestructura.Repositories
{
    public class UserContext : IUserContext
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public UserContext(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public string GetIpAddress()
        {
            var ipAddress = _httpContextAccessor.HttpContext?.Connection?.RemoteIpAddress;

            if (ipAddress == null)
                return "0.0.0.0";

            // Corrige el caso de ::1 u otras IPv6 mapeadas
            if (ipAddress.IsIPv4MappedToIPv6)
                ipAddress = ipAddress.MapToIPv4();

            return ipAddress.ToString();
        }
    }
}
