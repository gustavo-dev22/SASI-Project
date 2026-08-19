using SASI.Dominio.Modelo.Commons;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SASI.Dominio.Modelo
{
    public class SistemaVersion : AuditoriaBase
    {
        public int IdSistemaVersion { get; set; }
        public int SistemaId { get; set; }
        public Sistema Sistema { get; set; } = default!;
        public string Version { get; set; } = string.Empty; // Ej: 1.0.0
        public string? Changelog { get; set; }
        public string? Entorno { get; set; } // Ej: Producción, Desarrollo, QA
        public DateTime? FechaDespliegue { get; set; }
        public string? UsuarioDespliegue { get; set; }
    }
}
