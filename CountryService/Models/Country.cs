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

        public string Name_Official { get; set; }

        public string Name_Chinese { get; set; }

        public string CountryCode { get; set; }

        public string CountryCodeA3 { get; set; }

        public int Population { get; set; }

        public string FlagPermalink { get; set; }

        public string FlagDescription { get; set; }

        public string Region { get; set; }

        public string Subregion { get; set; }

        public double Area { get; set; }

        public string CoatOfArmsPermalink { get; set; }

        public string Languages { get; set; }
    }
}