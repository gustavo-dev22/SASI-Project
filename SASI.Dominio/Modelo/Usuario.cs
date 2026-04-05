using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SASI.Dominio.Modelo
{
    public class Usuario
    {
        public Guid IdUsuario { get; set; }
        public string NombreCompleto { get; set; }
        public string Correo { get; set; }

        public IEnumerable<UsuarioSistema> SistemasAsignados { get; set; }
    }
}
