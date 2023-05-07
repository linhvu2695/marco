#nullable disable

using System.ComponentModel.DataAnnotations;

namespace CountryService.Dtos
{
    public class UserLoginRequestDto
    {
        [Required]
        public string Email { get; set; }

        [Required]
        public string Password { get; set; }
    }
}