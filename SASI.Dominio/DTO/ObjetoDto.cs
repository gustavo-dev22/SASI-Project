using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SASI.Dominio.DTO
{
    public class ObjetoDto
    {
        public int IdObjeto { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string Tipo { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
        public string Titulo { get; set; } = string.Empty;
        public string Icono { get; set; } = string.Empty;
        public bool Activo { get; set; }
        public int Orden { get; set; }
        public int? IdPadre { get; set; }
    }
}
