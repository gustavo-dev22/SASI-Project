namespace SASI.Models.Requests
{
    public class EditarUsuarioRequest
    {
        public string Id { get; set; }
        public string NombreCompleto { get; set; }
        public string UserName { get; set; }
        public int OficinaId { get; set; }
        public string Email { get; set; }
        public bool Bloqueado { get; set; }
        public bool Activo { get; set; }
    }
}
