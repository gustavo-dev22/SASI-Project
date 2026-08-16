namespace SASI.Models.Requests
{
    public class NuevoUsuarioApiRequest
    {
        // Datos obligatorios para el IdentityUser
        public string Dni { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string NombreCompleto { get; set; } = string.Empty;

        // Opcionales según tu lógica
        public string? OficinaId { get; set; }
    }
}
