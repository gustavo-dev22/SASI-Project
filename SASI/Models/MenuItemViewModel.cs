namespace SASI.Models
{
    public class MenuItemViewModel
    {
        public int Id { get; set; }
        public string Nombre { get; set; }
        public string Url { get; set; }
        public string Icono { get; set; }
        public string Tipo { get; set; }
        public int? IdPadre { get; set; }
        public int Orden { get; set; }
    }
}
