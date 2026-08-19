using SASI.Dominio.Modelo.Commons;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SASI.Dominio.Modelo
{
    public class EstadoOperativoSistema : AuditoriaBase
    {
        public int IdEstadoOperativo { get; set; }
        public int SistemaId { get; set; }
        public Sistema Sistema { get; set; } = default!;
        public EstadoOperativo Estado { get; set; } = EstadoOperativo.Operativo;
        public string? Observacion { get; set; }
        public DateTime FechaRegistro { get; set; }
        public string? UsuarioRegistro { get; set; }
    }
}
