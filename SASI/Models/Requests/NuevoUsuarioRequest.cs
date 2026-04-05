namespace SASI.Models.Requests
{
    public class NuevoUsuarioRequest
    {
        public string NombreCompleto { get; set; }
        public string UserName { get; set; }
        public string Email { get; set; }
        public int OficinaId { get; set; }
    }
}
