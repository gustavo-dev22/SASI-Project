using SASI.Dominio.Modelo.Commons;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SASI.Dominio.Modelo
{
    public class Objeto : AuditoriaBase
    {
        public int IdObjeto { get; set; }
        public string Nombre { get; set; }
        public string Tipo { get; set; } // "Menu", "Submenu", "Item"

        [NotMapped]
        public TipoObjeto TipoObjeto
        {
            get => Enum.TryParse<TipoObjeto>(Tipo, out var tipoEnum) ? tipoEnum : default;
            set => Tipo = value.ToString();
        }

        public string? Url { get; set; }
        public string? Titulo { get; set; }
        public string Icono { get; set; }
        public bool Activo { get; set; }
        public int Orden { get; set; }
        public int? IdPadre { get; set; }
        public int IdSistema { get; set; }

        public Objeto? ObjetoPadre { get; set; }
        public IEnumerable<Objeto>? Hijos { get; set; }

        public Sistema Sistema { get; set; }
    }
}
