namespace SASI.Models
{
    public class HomeIndexViewModel
    {
        public List<MenuItemViewModel> MenuItems { get; set; } = new();
        public bool EsAdministrador { get; set; }
        public HomeDashboardViewModel? Dashboard { get; set; }
    }
}
