#nullable disable

using System.ComponentModel.DataAnnotations;

namespace CountryService.Models
{
    public class City
    {
        [Key]
        [Required]
        public int Id { get; set; }

        [Required]
        public string Name { get; set; }

        public int Population { get; set; }
    }
}