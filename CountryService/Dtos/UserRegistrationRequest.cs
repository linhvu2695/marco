#nullable disable

using System.ComponentModel.DataAnnotations;

namespace CountryService.Dtos
{
    public class UserRegistrationRequest
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        [RegularExpression(
            @"^(?=.*[A-Za-z])(?=.*\d)[A-Za-z\d]{8,}$", 
            ErrorMessage = "Password must contain at least 8 characters including letters and numbers."
            )
        ]
        public string Password { get; set; } = string.Empty;

        [Required]
        [Compare("Password")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}