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

        public Country? GetCountryById(int id)
        {
            return _context.Countries.FirstOrDefault(c => c.Id == id);
        }

        public Country? GetCountryByName(string name)
        {
            return _context.Countries.FirstOrDefault(c => c.Name == name);
        }

        public Country? GetCountryByCountryCode(string countryCode)
        {
            if (countryCode.Length < 2 || countryCode.Length > 3)
            {
                return null;
            }

            if (countryCode.Length == 3)
            {
                return _context.Countries.FirstOrDefault(c => c.CountryCodeA3 == countryCode);
            }
            return _context.Countries.FirstOrDefault(c => c.CountryCode == countryCode);
        }

        public Country_DbBO? GetCountryDbBOById(int id)
        {
            Country country = GetCountryById(id);
            if (country == null)
            {
                return null;
            }
            
            Country_DbBO country_DbBO = new Country_DbBO();
            country_DbBO.Country = country;
            country_DbBO.Cities = _context.Cities.Where(c => c.CountryId == country_DbBO.Country.Id).ToList();
            return country_DbBO;
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