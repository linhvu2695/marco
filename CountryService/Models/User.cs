#nullable disable

using System.ComponentModel.DataAnnotations;

namespace CountryService.Models
{
    public class User
    {
        [Key]
        [Required]
        public int Id { get; set; }
        
        [Required]
        public string Email { get; set; } = string.Empty;

        [Required]
        public byte[] PasswordHash { get; set; } = new byte[32];

        public byte[] PasswordSalt { get; set; } = new byte[32];

        public string PasswordEncryptionMethod { get; set; }

        public string? VerificationToken { get; set; }

        public DateTime? VerifiedAt { get; set; }

        public string? PasswordResetToken { get; set; }

        public DateTime? PasswordResetTokenExpiryTime { get; set; }
    }
}