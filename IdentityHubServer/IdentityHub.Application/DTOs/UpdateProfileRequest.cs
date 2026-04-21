using System;
using System.Collections.Generic;
using System.Text;

namespace IdentityHub.Application.DTOs
{
    public class UpdateProfileRequest
    {
        public string FullName { get; set; }
        public string Email { get; set; }
    }
}
