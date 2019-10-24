using DotNetty.Codecs.Http;
using DotNetty.Transport.Channels;
using JT1078.Gateway.Interfaces;
using System;
using System.Collections.Generic;
using System.Security.Principal;
using System.Text;

namespace JT1078.Gateway.SimpleServer
{
    public class CustomHttpMiddleware : IHttpMiddleware
    {
        public void Next(IChannelHandlerContext ctx, IFullHttpRequest req, IPrincipal principal)
        {
            Console.WriteLine("CustomHttpMiddleware");
        }
    }
}
