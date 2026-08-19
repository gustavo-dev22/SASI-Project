using Microsoft.AspNetCore.Mvc.Rendering;
using SASI.Dominio.DTO;

namespace SASI.Models
{
    public class SolicitudesAccesoViewModel
    {
        public List<SolicitudAccesoDto> Pendientes { get; set; } = new();
        public List<SolicitudAccesoDto> Respondidas { get; set; } = new();
        public List<SelectListItem> Sistemas { get; set; } = new();
    }
}
