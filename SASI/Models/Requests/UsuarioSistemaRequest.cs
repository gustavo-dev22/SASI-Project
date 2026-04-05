namespace SASI.Models.Requests
{
    public class UsuarioSistemaRequest
    {
        public Guid UsuarioId { get; set; }
        public int SistemaId { get; set; }
        public int RolId { get; set; }
        public bool EsPrincipal { get; set; }
    }
}
