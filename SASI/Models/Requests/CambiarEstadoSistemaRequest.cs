namespace SASI.Models.Requests
{
    public class CambiarEstadoSistemaRequest
    {
        public Guid UsuarioId { get; set; }
        public int SistemaId { get; set; }
        public int RolId { get; set; }
        public bool Activo { get; set; }
    }
}
