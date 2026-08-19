namespace SASI.Dominio.DTO
{
    public class ResumenSistemaReporteDto
    {
        public int IdSistema { get; set; }
        public string Codigo { get; set; } = string.Empty;
        public string Nombre { get; set; } = string.Empty;
        public bool Activo { get; set; }
        public int CantidadRoles { get; set; }
        public int CantidadUsuarios { get; set; }
        public int CantidadObjetos { get; set; }
    }
}
