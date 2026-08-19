using SASI.Dominio.Modelo.Commons;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SASI.Dominio.Modelo
{
    public class SistemaDocumento
    {
        public int IdSistemaDocumento { get; set; }
        public int SistemaId { get; set; }
        public Sistema Sistema { get; set; } = default!;
        public string Titulo { get; set; } = string.Empty;
        public string TipoDoc { get; set; } = string.Empty; // Manual, Diagrama, Acta
        public string? RutaArchivo { get; set; }
        public DateTime FechaSubida { get; set; } = DateTime.Now;
        public string? UsuarioSubida { get; set; }
    }
}
