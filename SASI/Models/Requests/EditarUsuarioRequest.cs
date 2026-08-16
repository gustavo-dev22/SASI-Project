namespace SASI.Models.Requests
{
    public class EditarUsuarioRequest
    {
        public string Id { get; set; } = string.Empty;
        public string NombreCompleto { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public int OficinaId { get; set; }
        public string Email { get; set; } = string.Empty;
        public bool Bloqueado { get; set; }
        public bool Activo { get; set; }
    }
}
