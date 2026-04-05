using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SASI.Dominio.Modelo
{
    public enum TipoObjeto
    {
        [Display(Name = "Menú")]
        Menu,

        [Display(Name = "Submenú")]
        Submenu
    }
}
