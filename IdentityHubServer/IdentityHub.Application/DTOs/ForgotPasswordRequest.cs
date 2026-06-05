using System;
using System.Collections.Generic;
using System.Text;

namespace IdentityHub.Application.DTOs
{
    public class ForgotPasswordRequest
    {
        public string Email { get; set; } = string.Empty;
    }
}

