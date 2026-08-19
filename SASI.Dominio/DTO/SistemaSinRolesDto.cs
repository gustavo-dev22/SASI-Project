namespace SASI.Dominio.DTO
{
    public class SistemaSinRolesDto
    {
        public int IdSistema { get; set; }
        public string Codigo { get; set; } = string.Empty;
        public string Nombre { get; set; } = string.Empty;
        public bool Activo { get; set; }
    }
}
