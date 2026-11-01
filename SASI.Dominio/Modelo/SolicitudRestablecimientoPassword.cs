using SASI.Dominio.Modelo.Commons;

namespace SASI.Dominio.Modelo
{
    public enum EstadoSolicitudPassword
    {
        Pendiente = 0,
        Atendida = 1,
        Rechazada = 2
    }

    // Solicitud de restablecimiento de contraseña hecha por el usuario desde el login
    // compartido. La atiende un administrador desde la consola SASI (proceso asistido).
    public class SolicitudRestablecimientoPassword : AuditoriaBase
    {
        public int IdSolicitud { get; set; }

        // Usuario de Identity (AspNetUsers). Sin FK: vive en otro DbContext.
        public Guid? UsuarioId { get; set; }

        // Lo que el usuario escribió (correo o nombre de usuario).
        public string Identificador { get; set; } = string.Empty;

        // Snapshot para que el admin vea los datos aunque el usuario luego cambie.
        public string? UserName { get; set; }
        public string? Email { get; set; }
        public string? NombreCompleto { get; set; }

        // Sistema desde el que se originó la solicitud (flujo SSO), si aplica.
        public int? SistemaId { get; set; }
        public string? ClientId { get; set; }

        public string? Mensaje { get; set; }

        public EstadoSolicitudPassword Estado { get; set; } = EstadoSolicitudPassword.Pendiente;

        public DateTime FechaSolicitud { get; set; } = DateTime.Now;
        public DateTime? FechaRespuesta { get; set; }
        public string? AtendidoPor { get; set; }
        public string? ComentarioRespuesta { get; set; }
    }
}
