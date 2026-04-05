using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SASI.Dominio.Modelo
{
    public class Oficina
    {
        public int IdOficina { get; set; }
        public string Nombre { get; set; }
        public string Sigla { get; set; }
        public int? IdOficinaPadre { get; set; }
        public bool Activo { get; set; } = true;
    }
}
