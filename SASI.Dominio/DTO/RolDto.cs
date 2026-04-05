namespace SASI.Dominio.DTO
{
    public class RolDto
    {
        public int IdRol { get; set; }
        public string NombreRol { get; set; }
        public bool Activo { get; set; }
        public bool EsPrincipal { get; set; }
        public List<ObjetoDto> Objetos { get; set; }
    }
}
