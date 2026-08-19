using SASI.Dominio.Modelo.Commons;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SASI.Dominio.Modelo
{
    public class SistemaContrato : AuditoriaBase
    {
        public int IdSistemaContrato { get; set; }
        public int SistemaId { get; set; }
        public Sistema Sistema { get; set; } = default!;
        public string? Proveedor { get; set; }
        public string? NroContrato { get; set; }
        public DateTime? FechaInicio { get; set; }
        public DateTime? FechaFin { get; set; }
        public decimal? CostoAnual { get; set; }
        public string? SLA_Detalle { get; set; }
    }
}
