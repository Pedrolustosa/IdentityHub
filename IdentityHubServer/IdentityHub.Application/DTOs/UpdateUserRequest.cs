using System.ComponentModel.DataAnnotations;

namespace IdentityHub.Application.DTOs
{
    public class UpdateUserRequest
    {
        [MaxLength(120)]
        public string FullName { get; set; } = string.Empty;

        [MaxLength(32)]
        public string? PhoneNumber { get; set; }

        public DateOnly? DateOfBirth { get; set; }

        public bool IsActive { get; set; }
    }
}
