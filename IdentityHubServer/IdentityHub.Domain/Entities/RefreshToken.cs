using System;
using System.Collections.Generic;
using System.Text;

namespace IdentityHub.Domain.Entities
{
    public class RefreshToken
    {
        public Guid Id { get; set; }

        public Guid SessionId { get; set; }

        public string TokenHash { get; set; } = string.Empty;

        public DateTime Expires { get; set; }

        public DateTime Created { get; set; }

        public bool IsRevoked { get; set; }

        public string UserId { get; set; } = string.Empty;

        public ApplicationUser User { get; set; } = null!;
    }
}
