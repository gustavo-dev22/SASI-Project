using SASI.Dominio.Modelo.Commons;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SASI.Dominio.Modelo
{
    public class Rol : AuditoriaBase
    {
        public int IdRol { get; set; }
        public string Nombre { get; set; } // Ej: "Administrador", "Lector"
        public int IdSistema { get; set; } // Relación con el sistema al que pertenece el rol
        public bool Activo { get; set; } = true;

        [ForeignKey("IdSistema")]
        public Sistema Sistema { get; set; }
    }
}
