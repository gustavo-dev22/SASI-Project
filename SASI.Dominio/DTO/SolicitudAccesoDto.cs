using SASI.Dominio.Modelo;

namespace SASI.Dominio.DTO
{
    public class SolicitudAccesoDto
    {
        public int IdSolicitud { get; set; }
        public Guid UsuarioId { get; set; }
        public string? EmailUsuario { get; set; }
        public string? NombreUsuario { get; set; }
        public int SistemaId { get; set; }
        public string NombreSistema { get; set; } = string.Empty;
        public int RolId { get; set; }
        public string NombreRol { get; set; } = string.Empty;
        public string? Justificacion { get; set; }
        public EstadoSolicitudAcceso Estado { get; set; }
        public DateTime FechaSolicitud { get; set; }
        public DateTime? FechaRespuesta { get; set; }
        public string? AprobadoPor { get; set; }
        public string? ComentarioRespuesta { get; set; }
    }
}
