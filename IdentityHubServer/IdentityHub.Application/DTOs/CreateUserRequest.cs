using System.ComponentModel.DataAnnotations;

namespace IdentityHub.Application.DTOs
{
    public class CreateUserRequest
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        [MinLength(7)]
        [MaxLength(128)]
        public string Password { get; set; } = string.Empty;

        [MaxLength(120)]
        public string FullName { get; set; } = string.Empty;
    }
}

