namespace SASI.Models
{
    public class SsoClienteConfig
    {
        public string ClientId { get; set; } = string.Empty;
        public int IdSistema { get; set; }
        public List<string> RedirectUris { get; set; } = new();
        public bool RequierePkce { get; set; } = true;
        public string? ClientSecret { get; set; }
    }
}
