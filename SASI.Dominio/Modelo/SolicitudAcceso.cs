using SASI.Dominio.Modelo.Commons;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SASI.Dominio.Modelo
{
    public class SolicitudAcceso : AuditoriaBase
    {
        public int IdSolicitud { get; set; }
        public Guid UsuarioId { get; set; }
        public int SistemaId { get; set; }
        public Sistema Sistema { get; set; } = default!;
        public int RolId { get; set; }
        public Rol Rol { get; set; } = default!;
        public string? Justificacion { get; set; }
        public EstadoSolicitudAcceso Estado { get; set; } = EstadoSolicitudAcceso.Pendiente;
        public DateTime FechaSolicitud { get; set; }
        public DateTime? FechaRespuesta { get; set; }
        public string? AprobadoPor { get; set; }
        public string? ComentarioRespuesta { get; set; }
    }
}
