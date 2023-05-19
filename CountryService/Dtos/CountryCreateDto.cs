#nullable disable

using CountryService.Models;

namespace CountryService.Dtos
{
    public class CountryCreateDto
    {
        public string Name { get; set; }
        public string OfficialName { get; set; }
        public string CountryCode { get; set; }
        public int Population { get; set; }
    }
}