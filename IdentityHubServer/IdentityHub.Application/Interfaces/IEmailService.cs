using System;
using System.Collections.Generic;
using System.Text;

namespace IdentityHub.Application.Interfaces
{
    public interface IEmailService
    {
        Task SendAsync(string to, string subject, string body);
    }
}
