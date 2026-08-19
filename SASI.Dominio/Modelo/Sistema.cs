using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using SASI.Dominio.Modelo.Commons;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SASI.Dominio.Modelo
{
    public class Sistema : AuditoriaBase
    {
        public int IdSistema { get; set; }
        public string Codigo { get; set; } = string.Empty; // Ej: SIS-001
        public string Nombre { get; set; } = string.Empty;
        public string Descripcion { get; set; } = string.Empty;
        public DateTime FechaRegistro { get; set; }
        public bool Activo { get; set; } = true;

        // ----- Gobernanza TI: Ficha ampliada del sistema -----
        public string? ResponsableFuncional { get; set; }
        public string? ResponsableTecnico { get; set; }
        public int? AreaDuenaId { get; set; }
        [ValidateNever]
        public Oficina? AreaDuena { get; set; }
        public DateTime? FechaPuestaProduccion { get; set; }
        public string? VersionActual { get; set; }
        public EstadoCicloVida EstadoCicloVida { get; set; } = EstadoCicloVida.EnDesarrollo;
        public int? RpoHoras { get; set; }
        public int? RtoHoras { get; set; }
        public string? PoliticaRespaldo { get; set; }
        public DateTime? FechaUltimaPruebaRestauracion { get; set; }

        [ValidateNever]
        public IEnumerable<UsuarioSistema> Usuarios { get; set; } = default!;

        [ValidateNever]
        public virtual IEnumerable<Rol> Roles { get; set; } = default!;

        [ValidateNever]
        public virtual IEnumerable<SistemaVersion> Versiones { get; set; } = default!;

        [ValidateNever]
        public virtual IEnumerable<SistemaContrato> Contratos { get; set; } = default!;

        [ValidateNever]
        public virtual IEnumerable<SistemaDocumento> Documentos { get; set; } = default!;
    }
}
