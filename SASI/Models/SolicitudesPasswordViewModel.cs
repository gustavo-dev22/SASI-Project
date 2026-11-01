using SASI.Dominio.DTO;

namespace SASI.Models
{
    public class SolicitudesPasswordViewModel
    {
        public List<SolicitudPasswordDto> Pendientes { get; set; } = new();
        public List<SolicitudPasswordDto> Atendidas { get; set; } = new();
        public List<SolicitudPasswordDto> Rechazadas { get; set; } = new();
    }
}
