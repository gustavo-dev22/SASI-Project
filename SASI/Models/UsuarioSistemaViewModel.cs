using Microsoft.AspNetCore.Mvc.Rendering;
using SASI.Dominio.DTO;
using X.PagedList;

namespace SASI.Models
{
    public class UsuarioSistemaViewModel
    {
        public int SistemaId { get; set; }
        public string CodigoSistema { get; set; } = string.Empty;
        public string NombreSistema { get; set; } = string.Empty;

        public IPagedList<UsuarioAsignadoDto> UsuariosAsignados { get; set; } = default!;
        public List<SelectListItem> RolesDisponibles { get; set; } = default!;
    }
}
