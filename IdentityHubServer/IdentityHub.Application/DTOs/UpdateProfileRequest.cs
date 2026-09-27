using System.ComponentModel.DataAnnotations;

namespace IdentityHub.Application.DTOs
{
    public class UpdateProfileRequest
    {
        [Required]
        [MaxLength(120)]
        public string FullName { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [MaxLength(32)]
        public string? PhoneNumber { get; set; }

        public DateOnly? DateOfBirth { get; set; }
    }
}
