namespace SASI.Dominio.DTO
{
    public class SistemaContratoDto
    {
        public int IdSistemaContrato { get; set; }
        public int SistemaId { get; set; }
        public string? Proveedor { get; set; }
        public string? NroContrato { get; set; }
        public DateTime? FechaInicio { get; set; }
        public DateTime? FechaFin { get; set; }
        public decimal? CostoAnual { get; set; }
        public string? SLA_Detalle { get; set; }

        // Cálculo de alerta de vencimiento
        public int? DiasParaVencer { get; set; }
        public string EstadoContrato { get; set; } = "Sin fecha fin";
    }
}
