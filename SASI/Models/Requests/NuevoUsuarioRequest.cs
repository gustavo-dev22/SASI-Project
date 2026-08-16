namespace SASI.Models.Requests
{
    public class NuevoUsuarioRequest
    {
        public string NombreCompleto { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public int OficinaId { get; set; }
    }
}
