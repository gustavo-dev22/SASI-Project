namespace SASI.Models.Requests
{
    public class NuevoUsuarioApiRequest
    {
        // Datos obligatorios para el IdentityUser
        public string Dni { get; set; }
        public string Email { get; set; }
        public string NombreCompleto { get; set; }

        // Opcionales según tu lógica
        public string? OficinaId { get; set; }
    }
}
