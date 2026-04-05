namespace SASI.Models
{
    public class SistemaViewModel
    {
        public int IdSistema { get; set; }
        public string Codigo { get; set; }
        public string Nombre { get; set; }
        public string Descripcion { get; set; }
        public DateTime FechaRegistro { get; set; }
        public int CantidadRoles { get; set; }
        public bool Estado { get; set; }
    }
}
