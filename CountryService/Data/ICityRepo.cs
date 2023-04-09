using CountryService.Models;

namespace CountryService.Data
{
    public interface ICityRepo
    {
        bool SaveChanges();

        IEnumerable<City> GetAllCities();

        City? GetCityById(int id);

        void CreateCity(City c);

        bool CityExists(int cityId);

        void DeleteCity(City city);

        void UpdateCity(City city);
    }
}
