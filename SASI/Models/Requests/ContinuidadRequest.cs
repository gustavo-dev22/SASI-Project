using System.ComponentModel.DataAnnotations;

namespace SASI.Models.Requests
{
    public class ContinuidadRequest
    {
        [Range(1, int.MaxValue, ErrorMessage = "El sistema es obligatorio.")]
        public int SistemaId { get; set; }

        [Range(1, 8760, ErrorMessage = "El RPO debe ser entre 1 y 8760 horas.")]
        public int? RpoHoras { get; set; }

        [Range(1, 8760, ErrorMessage = "El RTO debe ser entre 1 y 8760 horas.")]
        public int? RtoHoras { get; set; }

        [StringLength(500, ErrorMessage = "La política de respaldo no puede exceder 500 caracteres.")]
        public string? PoliticaRespaldo { get; set; }

        public DateTime? FechaUltimaPruebaRestauracion { get; set; }
    }
}
