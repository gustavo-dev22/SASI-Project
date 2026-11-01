using SASI.Dominio.Modelo;

namespace SASI.Dominio.DTO
{
    public class SolicitudPasswordDto
    {
        public int IdSolicitud { get; set; }
        public Guid? UsuarioId { get; set; }
        public string Identificador { get; set; } = string.Empty;
        public string? UserName { get; set; }
        public string? Email { get; set; }
        public string? NombreCompleto { get; set; }
        public string? SistemaNombre { get; set; }
        public string? ClientId { get; set; }
        public string? Mensaje { get; set; }
        public EstadoSolicitudPassword Estado { get; set; }
        public DateTime FechaSolicitud { get; set; }
        public DateTime? FechaRespuesta { get; set; }
        public string? AtendidoPor { get; set; }
        public string? ComentarioRespuesta { get; set; }
    }
}
