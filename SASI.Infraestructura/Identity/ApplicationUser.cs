using Microsoft.AspNetCore.Identity;
using SASI.Dominio.Modelo;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SASI.Infraestructura.Identity
{
    public class ApplicationUser : IdentityUser<Guid>
    {
        public string NombreCompleto { get; set; }

        // Campo personalizado
        public bool Activo { get; set; } = true; // Por defecto activo

        // Campos de auditoría
        public string AuditUsuarioCreacion { get; set; }
        public DateTime AuditFechaCreacion { get; set; }
        public string IpCreacion { get; set; }

        public string? AuditUsuarioModificacion { get; set; }
        public DateTime? AuditFechaModificacion { get; set; }
        public string? IpModificacion { get; set; }

        public int IntentosFallidosConsecutivos { get; set; } = 0;

        // Indica si el usuario debe cambiar su contraseña al iniciar sesión
        public bool DebeCambiarPassword { get; set; } = true;

        // Fecha del último cambio de contraseña
        public DateTime? FechaUltimoCambioPassword { get; set; } = DateTime.UtcNow;

        public int? IdOficina { get; set; }
    }
}
