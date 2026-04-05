namespace SASI.Models.Requests
{
    public class QuitarSistemaRequest
    {
        public Guid UsuarioId { get; set; }
        public int SistemaId { get; set; }
    }
}
