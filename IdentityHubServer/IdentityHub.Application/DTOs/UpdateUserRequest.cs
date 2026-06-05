using System;
using System.Collections.Generic;
using System.Text;

namespace IdentityHub.Application.DTOs
{
    public class UpdateUserRequest
    {
        public string FullName { get; set; } = string.Empty;
        public bool IsActive { get; set; }
    }
}

