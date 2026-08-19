namespace SASI.Dominio.DTO
{
    public class RolSinObjetosDto
    {
        public int IdRol { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public int IdSistema { get; set; }
        public string NombreSistema { get; set; } = string.Empty;
        public bool Activo { get; set; }
    }
}
