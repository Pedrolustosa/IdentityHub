using System;
using System.Collections.Generic;
using System.Text;

namespace IdentityHub.Domain.Entities
{
    public class UserSession
    {
        public Guid Id { get; set; }
        public string UserId { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime? RevokedAt { get; set; }
        public bool IsActive { get; set; }
    }
}
