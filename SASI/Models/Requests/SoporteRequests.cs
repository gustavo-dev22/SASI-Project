using System.ComponentModel.DataAnnotations;
using SASI.Dominio.Modelo;

namespace SASI.Models.Requests
{
    public class IncidenciaRequest
    {
        public int IdIncidencia { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "El sistema es obligatorio.")]
        public int SistemaId { get; set; }

        [Required(ErrorMessage = "El título es obligatorio.")]
        [StringLength(200, ErrorMessage = "El título no puede exceder 200 caracteres.")]
        public string Titulo { get; set; } = string.Empty;

        [StringLength(4000, ErrorMessage = "La descripción no puede exceder 4000 caracteres.")]
        public string? Descripcion { get; set; }

        public PrioridadIncidencia Prioridad { get; set; } = PrioridadIncidencia.Media;

        public EstadoIncidencia Estado { get; set; } = EstadoIncidencia.Abierta;

        [StringLength(200, ErrorMessage = "El responsable no puede exceder 200 caracteres.")]
        public string? Responsable { get; set; }
    }

    public class SolicitudAccesoRequest
    {
        [Range(1, int.MaxValue, ErrorMessage = "El sistema es obligatorio.")]
        public int SistemaId { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "El rol es obligatorio.")]
        public int RolId { get; set; }

        [StringLength(1000, ErrorMessage = "La justificación no puede exceder 1000 caracteres.")]
        public string? Justificacion { get; set; }
    }

    public class ResponderSolicitudRequest
    {
        [Range(1, int.MaxValue, ErrorMessage = "La solicitud es obligatoria.")]
        public int IdSolicitud { get; set; }

        [StringLength(1000, ErrorMessage = "El comentario no puede exceder 1000 caracteres.")]
        public string? Comentario { get; set; }
    }

    public class EstadoOperativoRequest
    {
        [Range(1, int.MaxValue, ErrorMessage = "El sistema es obligatorio.")]
        public int SistemaId { get; set; }

        public EstadoOperativo Estado { get; set; } = EstadoOperativo.Operativo;

        [StringLength(1000, ErrorMessage = "La observación no puede exceder 1000 caracteres.")]
        public string? Observacion { get; set; }
    }

    public class CambiarEstadoIncidenciaRequest
    {
        [Range(1, int.MaxValue, ErrorMessage = "La incidencia es obligatoria.")]
        public int Id { get; set; }

        public EstadoIncidencia Estado { get; set; }
    }
}
