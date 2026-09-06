namespace SASI.Dominio.DTO
{
    public class UsuarioAsignadoDto
    {
        public Guid UsuarioId { get; set; }
        public string Email { get; set; } = string.Empty;
        public string NombreCompleto { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public int RolId { get; set; }
        public string Rol { get; set; } = string.Empty;
        public DateTime FechaAsignacion { get; set; }
        public bool EsPrincipal { get; set; }

        public int? IdOficina { get; set; }
        public string? NombreOficina { get; set; }
        public string? SiglaOficina { get; set; }
    }
}
