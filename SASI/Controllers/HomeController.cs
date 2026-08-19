using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using SASI.Authorization;
using SASI.Models;
using SASI.Servicios;
using System.Diagnostics;

namespace SASI.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        private const string RolAdministrador = "Administrador";
        private const string RolAdministradorSeguridad = "Administrador de Seguridad";

        private readonly ILogger<HomeController> _logger;
        private readonly IDashboardServicio _dashboardServicio;

        public HomeController(ILogger<HomeController> logger, IDashboardServicio dashboardServicio)
        {
            _logger = logger;
            _dashboardServicio = dashboardServicio;
        }

        public async Task<IActionResult> Index()
        {
            var menuJson = HttpContext.Session.GetString("MenuUsuario");

            var menuItems = string.IsNullOrEmpty(menuJson)
                ? new List<MenuItemViewModel>()
                : JsonConvert.DeserializeObject<List<MenuItemViewModel>>(menuJson);

            var esAdministrador = User.IsInRole(RolAdministrador) || User.IsInRole(RolAdministradorSeguridad);

            var viewModel = new HomeIndexViewModel
            {
                MenuItems = menuItems ?? new List<MenuItemViewModel>(),
                EsAdministrador = esAdministrador,
                Dashboard = esAdministrador ? await _dashboardServicio.ObtenerTotalesAsync() : null
            };

            return View(viewModel);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
