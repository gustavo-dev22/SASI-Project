using Microsoft.AspNetCore.Mvc.Rendering;
using SASI.Dominio.Modelo;

namespace SASI.Models
{
    public class ObjetoViewModel
    {
        public int IdObjeto { get; set; }
        public string Nombre { get; set; }

        public string Tipo { get; set; } // "Menu", "Submenu", "Item"

        public TipoObjeto TipoObjeto
        {
            get => Enum.TryParse<TipoObjeto>(Tipo, out var tipoEnum) ? tipoEnum : default;
            set => Tipo = value.ToString();
        }

        public string? Url { get; set; }
        public string? Titulo { get; set; }
        public string? Icono { get; set; }
        public bool Activo { get; set; } = true;
        public int Orden { get; set; }

        public int? IdPadre { get; set; }
        public int IdSistema { get; set; }

        public IEnumerable<SelectListItem>? ObjetosPadre { get; set; }
    }
}
