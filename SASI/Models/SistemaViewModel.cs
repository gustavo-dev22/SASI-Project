namespace SASI.Models
{
    public class SistemaViewModel
    {
        public int IdSistema { get; set; }
        public string Codigo { get; set; } = string.Empty;
        public string Nombre { get; set; } = string.Empty;
        public string Descripcion { get; set; } = string.Empty;
        public DateTime FechaRegistro { get; set; }
        public int CantidadRoles { get; set; }
        public bool Estado { get; set; }
    }
}
