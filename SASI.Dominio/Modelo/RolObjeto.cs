using SASI.Dominio.Modelo.Commons;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SASI.Dominio.Modelo
{
    public class RolObjeto : AuditoriaBase
    {
        public int IdRolObjeto { get; set; }
        public int IdRol { get; set; }
        public int IdObjeto { get; set; }
        public bool Activo { get; set; } = true;

        public Rol Rol { get; set; }
        public Objeto Objeto { get; set; }
    }
}
