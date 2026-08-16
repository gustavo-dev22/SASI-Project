namespace SASI.Models.Response
{
    public class UsuarioBasicoResponse
    {
        public Guid IdUsuario { get; set; }
        public string NombreCompleto { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
    }
}
