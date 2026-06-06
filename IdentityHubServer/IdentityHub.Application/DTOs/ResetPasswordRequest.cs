using System.ComponentModel.DataAnnotations;

namespace IdentityHub.Application.DTOs
{
    public class ResetPasswordRequest
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        [MinLength(8)]
        public string Token { get; set; } = string.Empty;

        [Required]
        [MinLength(7)]
        [MaxLength(128)]
        public string NewPassword { get; set; } = string.Empty;
    }
}

