using System.ComponentModel.DataAnnotations;

namespace IdentityHub.Application.DTOs
{
    public class UpdateRolePermissionsRequest
    {
        [Required]
        [MinLength(1)]
        public List<string> Permissions { get; set; } = [];
    }
}

