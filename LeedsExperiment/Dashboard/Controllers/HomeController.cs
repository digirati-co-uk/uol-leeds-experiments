using Dashboard.Models;
using Microsoft.AspNetCore.Mvc;
using Preservation.API;
using System.Diagnostics;

namespace Dashboard.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> logger;
        private readonly IPreservation preservation;

        public HomeController(
            IPreservation preservation,
            ILogger<HomeController> logger)
        {
            this.preservation = preservation;
            this.logger = logger;
        }

        public IActionResult Index()
        {
            return View();
        }


        public async Task<IActionResult> ForecastAsync()
        {
            var forecasts = await preservation.GetWeatherForecasts();
            return View(forecasts[0]);
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
