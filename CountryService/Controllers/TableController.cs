using CountryService.Data;
using CountryService.Models;
using Microsoft.AspNetCore.Mvc;

namespace CountryService.Controllers
{
    public class TableController : Controller
    {
        private readonly ICountryRepo _countryRepo;
        private readonly ICityRepo _cityRepo;
        private readonly ILogger<TableController> _logger;

        public TableController(ICountryRepo countryRepo, ICityRepo cityRepo, ILogger<TableController> logger)
        {
            _countryRepo = countryRepo;
            _cityRepo = cityRepo;
            _logger = logger;
        }

        public IActionResult Countries()
        {
            _logger.LogInformation("--> Viewing Countries");
            IEnumerable<Country> oCountries = _countryRepo.GetAllCountries();
            return View(oCountries);
        }

        public IActionResult Cities()
        {
            _logger.LogInformation("--> Viewing Cities");
            IEnumerable<City> oCities = _cityRepo.GetAllCities();
            return View(oCities);
        }

        public IActionResult CountryDetail(int countryId)
        {
            return View(_countryRepo.GetCountryById(countryId, true));
        }
    }
}
