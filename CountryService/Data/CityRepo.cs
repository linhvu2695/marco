#nullable disable

using CountryService.Models;

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