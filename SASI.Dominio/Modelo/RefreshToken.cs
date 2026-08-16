namespace SASI.Dominio.Modelo
{
    public class RefreshToken
    {
        public int Id { get; set; }
        public Guid UsuarioId { get; set; }
        public string TokenHash { get; set; } = string.Empty;
        public DateTime ExpiresUtc { get; set; }
        public DateTime CreatedUtc { get; set; }
        public DateTime? RevokedUtc { get; set; }
        public string? ReplacedByTokenHash { get; set; }
    }
}
