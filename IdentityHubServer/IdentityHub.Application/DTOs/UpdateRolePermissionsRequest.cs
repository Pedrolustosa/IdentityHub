using System;
using System.Collections.Generic;
using System.Text;

namespace IdentityHub.Application.DTOs
{
    public class UpdateRolePermissionsRequest
    {
        public List<string> Permissions { get; set; }
    }
}
