using System.Web.Mvc;
using SASI.Dominio.DTO;

namespace SASI.Models
{
    public class UsuarioSistemaViewModel
    {
        public int SistemaId { get; set; }
        public string CodigoSistema { get; set; }
        public string NombreSistema { get; set; }

        public List<UsuarioAsignadoDto> UsuariosAsignados { get; set; }
        public List<SelectListItem> RolesDisponibles { get; set; }
    }
}
