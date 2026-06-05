using System;
using System.Collections.Generic;
using System.Text;

namespace IdentityHub.Application.DTOs
{
    public class UpdateProfileRequest
    {
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
    }
}

