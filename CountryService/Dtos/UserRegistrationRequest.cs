#nullable disable

using System.ComponentModel.DataAnnotations;

namespace CountryService.Dtos
{
    public class UserRegistrationRequest
    {
        [Required, EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required, MinLength(1)]
        public string Password { get; set; } = string.Empty;

        [Required, Compare("Password")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}