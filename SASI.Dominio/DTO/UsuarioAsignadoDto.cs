namespace SASI.Dominio.DTO
{
    public class UsuarioAsignadoDto
    {
        public Guid UsuarioId { get; set; }
        public string Email { get; set; }
        public string NombreCompleto { get; set; }
        public string UserName { get; set; }
        public string Rol { get; set; }
        public DateTime FechaAsignacion { get; set; }
    }
}
