#nullable disable

using CountryService.Models;

namespace CountryService.Dtos
{
    public class CountryCreateDto
    {
        public string Name { get; set; }
        public int Population { get; set; }
    }
}