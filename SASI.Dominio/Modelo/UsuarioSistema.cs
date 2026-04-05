using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SASI.Dominio.Modelo
{
    public class UsuarioSistema
    {
        public int IdUsuarioSistema { get; set; }

        public Guid UsuarioId { get; set; }

        public int SistemaId { get; set; }

        public Sistema Sistema { get; set; }

        public int RolId { get; set; }

        public Rol Rol { get; set; }

        public DateTime FechaAsignacion { get; set; }

        public bool Activo { get; set; }

        public bool EsPrincipal { get; set; }
    }
}
