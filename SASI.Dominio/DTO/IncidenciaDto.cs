using SASI.Dominio.Modelo;

namespace SASI.Dominio.DTO
{
    public class IncidenciaDto
    {
        public int IdIncidencia { get; set; }
        public int SistemaId { get; set; }
        public string NombreSistema { get; set; } = string.Empty;
        public string Titulo { get; set; } = string.Empty;
        public string Descripcion { get; set; } = string.Empty;
        public PrioridadIncidencia Prioridad { get; set; }
        public EstadoIncidencia Estado { get; set; }
        public string? Responsable { get; set; }
        public DateTime FechaReporte { get; set; }
        public DateTime? FechaAtencion { get; set; }
        public DateTime? FechaCierre { get; set; }
        public string? UsuarioReporte { get; set; }

        // Derivado: tiempo de atención en horas
        public double? TiempoAtencionHoras { get; set; }
    }
}
