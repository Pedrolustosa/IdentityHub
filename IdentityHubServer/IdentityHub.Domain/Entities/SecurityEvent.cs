using System;
using System.Collections.Generic;
using System.Text;

namespace IdentityHub.Domain.Entities
{
    public class SecurityEvent
    {
        public Guid Id { get; set; }
        public string UserId { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string Severity { get; set; } = "Medium";
        public string Status { get; set; } = "Open";
        public string Description { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }
}
