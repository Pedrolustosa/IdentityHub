using System;
using System.Collections.Generic;
using System.Text;

namespace IdentityHub.Application.DTOs
{
    public class UserResponse
    {
        public string Id { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public IList<string> Roles { get; set; } = [];
    }
}

