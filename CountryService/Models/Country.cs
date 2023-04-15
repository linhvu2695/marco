#nullable disable

using System.ComponentModel.DataAnnotations;

namespace CountryService.Models
{
    public class Country
    {
        [Key]
        [Required]
        public int Id { get; set; }

        [Required]
        public string Name { get; set; }

        public string OfficialName { get; set; }

        public int Population { get; set; }

        public HashSet<City> Cities { get; set; }
    }
}