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
        public string Nombre { get; set; }
        public string Tipo { get; set; }
        public string Url { get; set; }
        public string Titulo { get; set; }
        public string Icono { get; set; }
        public bool Activo { get; set; }
        public int Orden { get; set; }
        public int? IdPadre { get; set; }
    }
}
