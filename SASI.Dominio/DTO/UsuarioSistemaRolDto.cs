using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SASI.Dominio.DTO
{
    public class UsuarioSistemaRolDto
    {
        public int SistemaId { get; set; }
        public string SistemaNombre { get; set; }
        public bool SistemaActivo { get; set; }

        public int RolId { get; set; }
        public string RolNombre { get; set; }
        public bool RolActivo { get; set; }

        public bool UsuarioSistemaRolActivo { get; set; }

        public bool EsPrincipal { get; set; }

        public List<ObjetoDto> Objetos { get; set; } = new();
    }
}
