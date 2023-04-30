using CountryService.Data;
using CountryService.Models;
using Microsoft.AspNetCore.Mvc;

namespace CountryService.Controllers
{
    public class QuizController : Controller
    {
        private readonly ICountryRepo _countryRepo;
        private readonly ICityRepo _cityRepo;
        private readonly ILogger<QuizController> _logger;

        public QuizController(ICountryRepo countryRepo, ICityRepo cityRepo, ILogger<QuizController> logger)
        {
            _countryRepo = countryRepo;
            _cityRepo = cityRepo;
            _logger = logger;
        }

        public IActionResult Index()
        {
            _logger.LogInformation("--> Viewing Index");

            List<City> oCities = _cityRepo.GetAllCities().ToList();
            var random = new Random();
            City randomCity;
            String question;
            var answer = "";

            do
            {
                randomCity = oCities[random.Next(oCities.Count)];
                question = $"In which country is {randomCity.Name}";
                var country = _cityRepo.GetCountryByCityId(randomCity.Id);
                answer = country?.Name;
            } while (answer == null);
            Puzzle puzzle = new Puzzle(question, answer);

            return View(puzzle);
        }
    }
}