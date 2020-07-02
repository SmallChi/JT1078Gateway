using System;
using System.Collections.Generic;
using System.Net;
using System.Security.Principal;
using System.Text;

namespace JT1078.Gateway.Metadata
{
    public class JT1078HttpContext
    {
        public HttpListenerContext Context { get; }
        public IPrincipal User { get; }
        public JT1078HttpContext(HttpListenerContext context, IPrincipal user)
        {
            Context = context;
            User = user;
        }
    }
}
