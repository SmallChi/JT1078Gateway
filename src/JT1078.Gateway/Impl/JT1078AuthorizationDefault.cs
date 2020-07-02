using JT1078.Gateway.Abstractions;
using System;
using System.Collections.Generic;
using System.Net;
using System.Security.Claims;
using System.Security.Principal;
using System.Text;

namespace JT1078.Gateway.Impl
{
    class JT1078AuthorizationDefault : IJT1078Authorization
    {
        public bool Authorization(HttpListenerContext context, out IPrincipal principal)
        {
            var token = context.Request.QueryString.Get("token");
            if (!string.IsNullOrEmpty(token))
            {
                principal = new ClaimsPrincipal(new GenericIdentity(token));
                return true;
            }
            else
            {
                principal = null;
                return false;
            }
        }
    }
}
