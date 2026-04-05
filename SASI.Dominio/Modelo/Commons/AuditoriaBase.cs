using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SASI.Dominio.Modelo.Commons
{
    public abstract class AuditoriaBase
    {
        public string? AuditUsuarioCreacion { get; set; }

        public DateTime? AuditFechaCreacion { get; set; }

        public string? IpCreacion { get; set; }

        public string? AuditUsuarioModificacion { get; set; }

        public DateTime? AuditFechaModificacion { get; set; }

        public string? IpModificacion { get; set; }
    }
}
