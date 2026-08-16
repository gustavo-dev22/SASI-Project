namespace SASI.Models
{
    public class MenuItemViewModel
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
        public string Icono { get; set; } = string.Empty;
        public string Tipo { get; set; } = string.Empty;
        public int? IdPadre { get; set; }
        public int Orden { get; set; }
    }
}
