#nullable disable

using System.ComponentModel.DataAnnotations;

namespace CountryService.Dtos
{
    public class TokenRequestDto
    {
        [Required]
        public string Token { get; set; }
    
        [Required]
        public string RefreshToken { get; set; }
    }
}