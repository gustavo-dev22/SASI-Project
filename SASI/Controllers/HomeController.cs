using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using SASI.Models;
using System.Diagnostics;

namespace SASI.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        public IActionResult Index()
        {
            var menuJson = HttpContext.Session.GetString("MenuUsuario");

            var menuItems = string.IsNullOrEmpty(menuJson)
                ? new List<MenuItemViewModel>()
                : JsonConvert.DeserializeObject<List<MenuItemViewModel>>(menuJson);

            return View(menuItems);
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
