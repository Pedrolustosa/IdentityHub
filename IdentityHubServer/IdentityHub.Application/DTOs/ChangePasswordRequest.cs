using System.ComponentModel.DataAnnotations;

namespace IdentityHub.Application.DTOs
{
    public class ChangePasswordRequest
    {
        [Required]
        [MinLength(7)]
        [MaxLength(128)]
        public string CurrentPassword { get; set; } = string.Empty;

        [Required]
        [MinLength(7)]
        [MaxLength(128)]
        public string NewPassword { get; set; } = string.Empty;
    }
}

