using System;
using System.Collections.Generic;
using System.Net;
using System.Security.Principal;
using System.Text;

namespace JT1078.Gateway.Abstractions
{
    public interface IJT1078Authorization
    {
        bool Authorization(HttpListenerContext context, out IPrincipal principal);
    }
}
