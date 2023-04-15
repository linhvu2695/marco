using CountryService.Models;

namespace CountryService.Data
{
    public interface ICountryRepo
    {
        bool SaveChanges();

        IEnumerable<Country> GetAllCountries();

        Country? GetCountryById(int id);

        IEnumerable<City> GetCitiesFromCountry(int countryId);

        void CreateCountry(Country c);

        bool CountryExists(int countryId);

        void DeleteCountry(Country country);

        void UpdateCountryPopulation(int countryId, int updatedPopulation);
    }
}
