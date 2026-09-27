using Microsoft.AspNetCore.Identity;

namespace IdentityHub.Domain.Entities
{
    public class ApplicationUser : IdentityUser
    {
        public string? FullName { get; set; }

        /// <summary>
        /// Optional calendar date of birth (date-only; no time component).
        /// Phone number is stored via IdentityUser.PhoneNumber.
        /// </summary>
        public DateOnly? DateOfBirth { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public bool IsActive { get; set; } = true;
        public bool IsDeleted { get; set; }
        public DateTime? DeletedAt { get; set; }
        public string? DeletedBy { get; set; }
        public int PermissionVersion { get; set; } = 1;
    }
}
