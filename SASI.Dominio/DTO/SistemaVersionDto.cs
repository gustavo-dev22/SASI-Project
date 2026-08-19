namespace SASI.Dominio.DTO
{
    public class SistemaVersionDto
    {
        public int IdSistemaVersion { get; set; }
        public int SistemaId { get; set; }
        public string Version { get; set; } = string.Empty;
        public string? Changelog { get; set; }
        public string? Entorno { get; set; }
        public DateTime? FechaDespliegue { get; set; }
        public string? UsuarioDespliegue { get; set; }
    }
}
