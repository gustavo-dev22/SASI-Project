using Microsoft.EntityFrameworkCore;
using SASI.Dominio.Modelo;
using SASI.Models;
using SistemaConvocatorias.Infraestructura.Datos;
using System.Security.Cryptography;
using System.Text;

namespace SASI.Servicios
{
    // Registra/actualiza los clientes SSO declarados en configuración (Sso:Clientes).
    public static class SsoClienteSeeder
    {
        public static async Task SembrarAsync(SasiDbContext db, IConfiguration config, ILogger logger)
        {
            var clientes = config.GetSection("Sso:Clientes").Get<List<SsoClienteConfig>>() ?? new List<SsoClienteConfig>();

            foreach (var c in clientes)
            {
                if (string.IsNullOrWhiteSpace(c.ClientId))
                    continue;

                if (!await db.Sistemas.AnyAsync(s => s.IdSistema == c.IdSistema))
                {
                    logger.LogWarning("SSO: se omite el cliente {ClientId} porque el sistema {SistemaId} no existe.", c.ClientId, c.IdSistema);
                    continue;
                }

                var redirects = string.Join(";", c.RedirectUris
                    .Where(u => !string.IsNullOrWhiteSpace(u))
                    .Select(u => u.Trim()));

                var existente = await db.SistemaClientes.FirstOrDefaultAsync(x => x.ClientId == c.ClientId);
                if (existente == null)
                {
                    db.SistemaClientes.Add(new SistemaCliente
                    {
                        ClientId = c.ClientId,
                        IdSistema = c.IdSistema,
                        RedirectUris = redirects,
                        RequierePkce = c.RequierePkce,
                        Activo = true,
                        ClientSecretHash = string.IsNullOrWhiteSpace(c.ClientSecret) ? null : Hash(c.ClientSecret)
                    });
                }
                else
                {
                    existente.IdSistema = c.IdSistema;
                    existente.RedirectUris = redirects;
                    existente.RequierePkce = c.RequierePkce;
                    if (!string.IsNullOrWhiteSpace(c.ClientSecret))
                        existente.ClientSecretHash = Hash(c.ClientSecret);
                }
            }

            await db.SaveChangesAsync();
        }

        private static string Hash(string valor)
            => Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(valor)));
    }
}
