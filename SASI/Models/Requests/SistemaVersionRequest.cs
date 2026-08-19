using System.ComponentModel.DataAnnotations;

namespace SASI.Models.Requests
{
    public class SistemaVersionRequest
    {
        public int IdSistemaVersion { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "El sistema es obligatorio.")]
        public int SistemaId { get; set; }

        [Required(ErrorMessage = "La versión es obligatoria.")]
        [StringLength(50, ErrorMessage = "La versión no puede exceder 50 caracteres.")]
        public string Version { get; set; } = string.Empty;

        [StringLength(4000, ErrorMessage = "El changelog no puede exceder 4000 caracteres.")]
        public string? Changelog { get; set; }

        [StringLength(50, ErrorMessage = "El entorno no puede exceder 50 caracteres.")]
        public string? Entorno { get; set; }

        public DateTime? FechaDespliegue { get; set; }
    }
}
