#nullable disable

using CountryService.Models;
using Microsoft.EntityFrameworkCore;
using Nest;

namespace CountryService.Data
{
    public class CountryRepo : ICountryRepo
    {
        private readonly AppDbContext _context;
        private readonly IElasticClient _elasticClient;

        public CountryRepo(AppDbContext context, IElasticClient elasticClient)
        {
            _context = context;
            _elasticClient = elasticClient;
        }

        public bool CountryExists(int countryId)
        {
            return _context.Countries.Any(c => c.Id == countryId);
        }

        public void CreateCountry(Country c)
        {
            if (c == null)
            {
                throw new ArgumentNullException(nameof(c));
            } 

            _context.Countries.Add(c);
        }

        public void DeleteCountry(Country country)
        {
            _context.Countries.Remove(country);
        }

        public IEnumerable<Country> GetAllCountries()
        {
            return _context.Countries.ToList();
        }

        public IEnumerable<City> GetCitiesFromCountry(int countryId)
        {
            var countryItem = _context.Countries.Include(c => c.Cities).FirstOrDefault(c => c.Id == countryId);
            return countryItem.Cities;
        }

        public Country? GetCountryById(int id)
        {
            return _context.Countries.FirstOrDefault(c => c.Id == id);
        }

        public Country? GetCountryByName(string name)
        {
            return _context.Countries.FirstOrDefault(c => c.Name == name);
        }

        public bool SaveChanges()
        {
            return _context.SaveChanges() >= 0;
        }

        public void UpdateCountryPopulation(int countryId, int updatedPopulation)
        {
            var countryItem = _context.Countries.FirstOrDefault(c => c.Id == countryId);
            countryItem.Population = updatedPopulation;
        }
    }
}