using System.ComponentModel.DataAnnotations;

namespace SASI.Models.Requests
{
    public class SistemaContratoRequest
    {
        public int IdSistemaContrato { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "El sistema es obligatorio.")]
        public int SistemaId { get; set; }

        [StringLength(200, ErrorMessage = "El proveedor no puede exceder 200 caracteres.")]
        public string? Proveedor { get; set; }

        [StringLength(100, ErrorMessage = "El número de contrato no puede exceder 100 caracteres.")]
        public string? NroContrato { get; set; }

        public DateTime? FechaInicio { get; set; }

        [CustomValidation(typeof(SistemaContratoRequest), nameof(ValidarFechas))]
        public DateTime? FechaFin { get; set; }

        [Range(0, 9999999999.99, ErrorMessage = "El costo anual no es válido.")]
        public decimal? CostoAnual { get; set; }

        [StringLength(2000, ErrorMessage = "El detalle del SLA no puede exceder 2000 caracteres.")]
        public string? SLA_Detalle { get; set; }

        public static ValidationResult? ValidarFechas(DateTime? fechaFin, ValidationContext context)
        {
            var instance = (SistemaContratoRequest)context.ObjectInstance;
            if (instance.FechaInicio.HasValue && fechaFin.HasValue && fechaFin.Value < instance.FechaInicio.Value)
            {
                return new ValidationResult("La fecha de fin no puede ser anterior a la fecha de inicio.");
            }
            return ValidationResult.Success;
        }
    }
}
