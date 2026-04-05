using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SASI.Dominio.DTO
{
    public class UsuarioConRolesDto
    {
        public Guid UsuarioId { get; set; }
        public string Email { get; set; }
        public string UserName { get; set; }
        public string NombreCompleto { get; set; }
        public List<string> Roles { get; set; } = new();
        public DateTime FechaAsignacion { get; set; }
    }
}
