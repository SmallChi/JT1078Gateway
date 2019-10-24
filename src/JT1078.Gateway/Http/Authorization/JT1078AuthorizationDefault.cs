using DotNetty.Codecs.Http;
using JT1078.Gateway.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Security.Principal;
using System.Text;

namespace JT1078.Gateway.Http.Authorization
{
    class JT1078AuthorizationDefault : IJT1078Authorization
    {
        public bool Authorization(IFullHttpRequest request, out IPrincipal principal)
        {
            var uriSpan = request.Uri.AsSpan();
            var uriParamStr = uriSpan.Slice(uriSpan.IndexOf('?')+1).ToString().ToLower();
            var uriParams = uriParamStr.Split('&');
            var tokenParam = uriParams.FirstOrDefault(m => m.Contains("token"));
            if (!string.IsNullOrEmpty(tokenParam))
            {
                principal = new ClaimsPrincipal(new GenericIdentity(tokenParam.Split('=')[1]));
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
