#nullable disable

using System.Data;
using CountryService.Models;
using Microsoft.EntityFrameworkCore;

namespace CountryService.Data
{
    public class CityRepo : ICityRepo
    {
        private readonly AppDbContext _context;

        public CityRepo(AppDbContext context)
        {
            _context = context;
        }

        public bool CityExists(int cityId)
        {
            return _context.Cities.Any(c => c.Id == cityId);
        }

        public void CreateCity(City c)
        {
            if (c == null)
            {
                throw new ArgumentNullException(nameof(c));
            } 

            _context.Cities.Add(c);
        }

        public void DeleteCity(City city)
        {
            _context.Cities.Remove(city);
        }

        public IEnumerable<City> GetAllCities()
        {
            return _context.Cities.ToList();
        }

        public City? GetCityById(int id)
        {
            return _context.Cities.FirstOrDefault(c => c.Id == id);
        }

        public Country GetCountryByCityId(int cityId)
        {
            // Retrieve CountryId
            int countryId = 0;
            String sQuery = $"SELECT CountryId FROM Cities WHERE Id = {cityId}";
            using (var command = _context.Database.GetDbConnection().CreateCommand())
            {
                command.CommandText = sQuery;
                command.CommandType = CommandType.Text;
                _context.Database.OpenConnection();
                using (var result = command.ExecuteReader())
                {
                    while (result.Read())
                    {
                        countryId = result.GetInt32(0);
                    }
                }
            }

            if (countryId != 0)
            {
                return _context.Countries.FirstOrDefault(c => c.Id == countryId);
            }
            return null;
        }

        public bool SaveChanges()
        {
            return _context.SaveChanges() >= 0;
        }

        public void UpdateCity(City city)
        {
            throw new NotImplementedException();
        }
    }
}