using System.ComponentModel.DataAnnotations;

namespace IdentityHub.Application.DTOs
{
    public class RefreshTokenRequest
    {
        [Required]
        [MinLength(16)]
        public string RefreshToken { get; set; } = string.Empty;
    }
}
