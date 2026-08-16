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
        public string NombreCompleto { get; set; } = string.Empty;
        public string Correo { get; set; } = string.Empty;

        public IEnumerable<UsuarioSistema> SistemasAsignados { get; set; } = default!;
    }
}
