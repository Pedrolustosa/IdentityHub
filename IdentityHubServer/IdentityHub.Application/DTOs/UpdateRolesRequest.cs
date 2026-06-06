using System.ComponentModel.DataAnnotations;

namespace IdentityHub.Application.DTOs
{
    public class UpdateRolesRequest
    {
        [Required]
        [MinLength(1)]
        public List<string> Roles { get; set; } = [];
    }
}

