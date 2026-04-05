using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace SASI.Controllers
{
    public class ErrorController : Controller
    {
        private readonly ILogger<ErrorController> _logger;
        public ErrorController(ILogger<ErrorController> logger)
        {
            _logger = logger;
        }

        [ApiExplorerSettings(IgnoreApi = true)]
        public IActionResult Error404()
        {
            _logger.LogWarning("Error 404 - Página no encontrada: {Path}", HttpContext.Request.Path);
            return View();
        }

        [ApiExplorerSettings(IgnoreApi = true)]
        public IActionResult Error500()
        {
            var exceptionDetails = HttpContext.Features.Get<IExceptionHandlerPathFeature>();
            if (exceptionDetails != null)
            {
                _logger.LogError(exceptionDetails.Error, "Error 500 - Excepción en: {Path}", exceptionDetails.Path);
            }

            return View();
        }

        [ApiExplorerSettings(IgnoreApi = true)]
        public IActionResult GeneralError(int statusCode)
        {
            return View("ErrorGeneral", statusCode);
        }
    }
}
