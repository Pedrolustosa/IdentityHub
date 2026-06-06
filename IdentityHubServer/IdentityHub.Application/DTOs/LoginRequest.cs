using System.ComponentModel.DataAnnotations;

namespace IdentityHub.Application.DTOs
{
    public class LoginRequest
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        [MinLength(7)]
        [MaxLength(128)]
        public string Password { get; set; } = string.Empty;
    }
}

