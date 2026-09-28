using SASI.Dominio.Modelo.Commons;

namespace SASI.Dominio.Modelo
{
    // Cliente (sistema externo o consola) autorizado a iniciar sesión vía SSO.
    public class SistemaCliente : AuditoriaBase
    {
        public int IdSistemaCliente { get; set; }
        public int IdSistema { get; set; }
        public string ClientId { get; set; } = string.Empty;
        public string? ClientSecretHash { get; set; }
        public string RedirectUris { get; set; } = string.Empty; // separadas por ';'
        public bool RequierePkce { get; set; } = true;
        public bool Activo { get; set; } = true;

        public Sistema Sistema { get; set; } = default!;
    }
}
