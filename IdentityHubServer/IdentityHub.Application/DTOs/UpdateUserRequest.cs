using System.ComponentModel.DataAnnotations;

namespace IdentityHub.Application.DTOs
{
    public class UpdateUserRequest
    {
        [MaxLength(120)]
        public string FullName { get; set; } = string.Empty;
        public bool IsActive { get; set; }
    }
}

