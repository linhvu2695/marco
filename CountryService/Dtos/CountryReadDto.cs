#nullable disable

using CountryService.Models;

namespace CountryService.Dtos
{
    public class CountryReadDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int Population { get; set; }
    }
}