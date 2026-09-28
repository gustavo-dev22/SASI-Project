namespace SASI.Dominio.Modelo
{
    // Authorization code efímero y de un solo uso para el flujo SSO.
    public class AuthCode
    {
        public int IdAuthCode { get; set; }
        public string CodeHash { get; set; } = string.Empty; // SHA-256 (base64) del code
        public string ClientId { get; set; } = string.Empty;
        public Guid UsuarioId { get; set; }
        public int SistemaId { get; set; }
        public string? RedirectUri { get; set; }
        public string? CodeChallenge { get; set; }
        public string? CodeChallengeMethod { get; set; } // S256
        public DateTime ExpiraUtc { get; set; }
        public DateTime? UsadoUtc { get; set; }
        public DateTime CreatedUtc { get; set; }
    }
}
