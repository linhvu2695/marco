using System.Text;
using CountryService.Constants;
using CountryService.Data;
using CountryService.Models;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;

namespace CountryService.Controllers
{
    public class QuizController : Controller
    {
        private readonly ICountryRepo _countryRepo;
        private readonly ICityRepo _cityRepo;
        private readonly ILogger<QuizController> _logger;
        private readonly IConfiguration _configuration;

        public QuizController(ICountryRepo countryRepo, ICityRepo cityRepo, ILogger<QuizController> logger, IConfiguration configuration)
        {
            _countryRepo = countryRepo;
            _cityRepo = cityRepo;
            _logger = logger;
            _configuration = configuration;
        }

        public IActionResult Cities()
        {
            _logger.LogInformation("--> Viewing Cities quiz");

            List<City> oCities = _cityRepo.GetAllCities().ToList();
            var random = new Random();
            City randomCity;
            String question;
            var answer = "";

            do
            {
                randomCity = oCities[random.Next(oCities.Count)];
                question = $"In which country is {randomCity.Name}?";
                var country = _cityRepo.GetCountryByCityId(randomCity.Id);
                answer = country?.Name;
            } while (answer == null);
            Puzzle puzzle = new Puzzle(question, answer);

            return View(puzzle);
        }

        public IActionResult Flags()
        {
            _logger.LogInformation("--> Viewing Flags quiz");

            List<Country> oCountries = _countryRepo.GetAllCountries().ToList();
            var random = new Random();
            Country randomCountry;
            String question = "This flag belongs to which country?";

            randomCountry = oCountries[random.Next(oCountries.Count)];
            var answer = randomCountry.Name;
            Puzzle flagPuzzle = new FlagPuzzle(question, answer, randomCountry.FlagPermalink);
            
            return View(flagPuzzle);
        }
    }
}