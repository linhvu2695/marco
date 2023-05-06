#nullable disable

namespace CountryService.Models
{
    public class Country_DbBO
    {
        public Country Country { get; set; }

        public IEnumerable<City> Cities { get; set; } = null;
    }
}