using Microsoft.AspNetCore.Mvc.Rendering;
using SASI.Dominio.DTO;

namespace SASI.Models
{
    public class UsuarioSistemaViewModel
    {
        public int SistemaId { get; set; }
        public string CodigoSistema { get; set; } = string.Empty;
        public string NombreSistema { get; set; } = string.Empty;

        public List<UsuarioAsignadoDto> UsuariosAsignados { get; set; } = default!;
        public List<SelectListItem> RolesDisponibles { get; set; } = default!;
    }
}
