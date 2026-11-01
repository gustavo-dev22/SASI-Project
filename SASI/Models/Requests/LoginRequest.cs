namespace SASI.Models.Requests
{
    public class LoginRequest
    {
        public string UserName { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;

        // Opcionales: permiten validar que el usuario tenga acceso al sistema destino.
        // Si se envían ambos, ClientId tiene prioridad.
        public int? SistemaId { get; set; }
        public string? ClientId { get; set; }
    }
}
