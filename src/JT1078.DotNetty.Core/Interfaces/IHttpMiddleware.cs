using DotNetty.Codecs.Http;
using DotNetty.Transport.Channels;
using System;
using System.Collections.Generic;
using System.Security.Principal;
using System.Text;

namespace JT1078.DotNetty.Core.Interfaces
{
    public interface IHttpMiddleware
    {
        void Next(IChannelHandlerContext ctx, IFullHttpRequest req, IPrincipal principal);
    }
}
