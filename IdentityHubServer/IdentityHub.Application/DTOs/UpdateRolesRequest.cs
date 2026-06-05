using System;
using System.Collections.Generic;
using System.Text;

namespace IdentityHub.Application.DTOs
{
    public class UpdateRolesRequest
    {
        public List<string> Roles { get; set; } = [];
    }
}

