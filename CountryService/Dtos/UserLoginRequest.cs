#nullable disable

using System.ComponentModel.DataAnnotations;

namespace CountryService.Dtos
{
    public class UserLoginRequest
    {
        [Required, EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required, MinLength(1)]
        public string Password { get; set; } = string.Empty;
    }
}