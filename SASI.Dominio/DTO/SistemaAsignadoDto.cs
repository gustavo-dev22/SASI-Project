using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SASI.Dominio.DTO
{
    public class SistemaAsignadoDto
    {
        public int SistemaId { get; set; }
        public string NombreSistema { get; set; } = string.Empty;
        public string NombreRol { get; set; } = string.Empty;
        public int RolId { get; set; }
        public DateTime FechaAsignacion { get; set; }
        public bool Activo { get; set; }
        public bool EsPrincipal { get; set; }
    }
}
