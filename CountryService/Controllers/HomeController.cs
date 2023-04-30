using System.Diagnostics;
using CountryService.Models;
using Microsoft.AspNetCore.Mvc;

namespace CountryService.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        public IActionResult Index()
        {
            _logger.LogInformation("--> Viewing Homepage");
            return View();
        }

        public IActionResult Privacy()
        {
            _logger.LogInformation("--> Viewing Privacy");
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
        }
}