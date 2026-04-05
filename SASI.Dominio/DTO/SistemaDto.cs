namespace SASI.Dominio.DTO
{
    public class SistemaDto
    {
        public int Id { get; set; }
        public string Nombre { get; set; }
        public bool Activo { get; set; }
        public List<RolDto> Roles { get; set; }
    }
}
