using SASI.Dominio.Modelo.Commons;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SASI.Dominio.Modelo
{
    public class Sistema : AuditoriaBase
    {
        public int IdSistema { get; set; }
        public string Codigo { get; set; } = string.Empty; // Ej: SIS-001
        public string Nombre { get; set; } = string.Empty;
        public string Descripcion { get; set; } = string.Empty;
        public DateTime FechaRegistro { get; set; }
        public bool Activo { get; set; } = true;

        public IEnumerable<UsuarioSistema> Usuarios { get; set; } = default!;

        public virtual IEnumerable<Rol> Roles { get; set; } = default!;
    }
}
