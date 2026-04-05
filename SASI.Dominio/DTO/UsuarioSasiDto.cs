namespace SASI.Dominio.DTO
{
    public class UsuarioSasiDto
    {
        public string Id { get; set; }
        public string NombreCompleto { get; set; }
        public string UserName { get; set; }
        public string Email { get; set; }
        public bool Activo { get; set; }
        public List<SistemaDto> Sistemas { get; set; }
    }
}
