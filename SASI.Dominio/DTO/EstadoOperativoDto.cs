using SASI.Dominio.Modelo;

namespace SASI.Dominio.DTO
{
    public class EstadoOperativoDto
    {
        public int IdEstadoOperativo { get; set; }
        public int SistemaId { get; set; }
        public string NombreSistema { get; set; } = string.Empty;
        public EstadoOperativo Estado { get; set; }
        public string? Observacion { get; set; }
        public DateTime FechaRegistro { get; set; }
        public string? UsuarioRegistro { get; set; }
    }
}
