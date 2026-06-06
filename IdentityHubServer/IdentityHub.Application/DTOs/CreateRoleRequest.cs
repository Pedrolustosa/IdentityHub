using System.ComponentModel.DataAnnotations;

namespace IdentityHub.Application.DTOs
{
    public class CreateRoleRequest
    {
        [Required]
        [MaxLength(64)]
        public string Name { get; set; } = string.Empty;
    }
}

