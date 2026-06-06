using System.ComponentModel.DataAnnotations;

namespace IdentityHub.Application.DTOs
{
    public class UpdateRoleRequest
    {
        [Required]
        [MaxLength(64)]
        public string Name { get; set; } = string.Empty;
    }
}

