using SASI.Dominio.Modelo.Commons;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SASI.Dominio.Modelo
{
    public class Incidencia : AuditoriaBase
    {
        public int IdIncidencia { get; set; }
        public int SistemaId { get; set; }
        public Sistema Sistema { get; set; } = default!;
        public string Titulo { get; set; } = string.Empty;
        public string Descripcion { get; set; } = string.Empty;
        public PrioridadIncidencia Prioridad { get; set; } = PrioridadIncidencia.Media;
        public EstadoIncidencia Estado { get; set; } = EstadoIncidencia.Abierta;
        public string? Responsable { get; set; }
        public DateTime FechaReporte { get; set; }
        public DateTime? FechaAtencion { get; set; }
        public DateTime? FechaCierre { get; set; }
        public string? UsuarioReporte { get; set; }
    }
}
