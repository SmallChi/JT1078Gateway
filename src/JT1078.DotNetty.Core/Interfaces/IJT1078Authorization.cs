using DotNetty.Codecs.Http;
using System;
using System.Collections.Generic;
using System.Security.Principal;
using System.Text;

namespace JT1078.DotNetty.Core.Interfaces
{
    public interface IJT1078Authorization
    {
        bool Authorization(IFullHttpRequest request, out IPrincipal principal);
    }
}
