using System;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;
using DotNetty.Buffers;
using DotNetty.Codecs.Http;
using DotNetty.Codecs.Http.WebSockets;
using DotNetty.Common.Utilities;
using DotNetty.Transport.Channels;
using static DotNetty.Codecs.Http.HttpVersion;
using static DotNetty.Codecs.Http.HttpResponseStatus;
using Microsoft.Extensions.Logging;
using JT1078.DotNetty.Core.Session;
using System.Text.RegularExpressions;
using JT1078.DotNetty.Core.Interfaces;

namespace JT1078.DotNetty.Http.Handlers
{
    public sealed class JT1078HttpServerHandler : SimpleChannelInboundHandler<object>
    {
        const string WebsocketPath = "/jt1078live";
        WebSocketServerHandshaker handshaker;
        private static readonly AsciiString ServerName = AsciiString.Cached("JT1078Netty");
        private static readonly AsciiString DateEntity = HttpHeaderNames.Date;
        private static readonly AsciiString ServerEntity = HttpHeaderNames.Server;

        private readonly ILogger<JT1078HttpServerHandler> logger;

        private readonly JT1078HttpSessionManager jT1078HttpSessionManager;

        private readonly IJT1078Authorization iJT1078Authorization;

        private readonly IHttpMiddleware httpMiddleware;

        public JT1078HttpServerHandler(
            JT1078HttpSessionManager jT1078HttpSessionManager,
            IJT1078Authorization iJT1078Authorization,
            ILoggerFactory loggerFactory,
            IHttpMiddleware httpMiddleware = null)
        {
            this.jT1078HttpSessionManager = jT1078HttpSessionManager;
            this.iJT1078Authorization = iJT1078Authorization;
            this.httpMiddleware = httpMiddleware;
            logger = loggerFactory.CreateLogger<JT1078HttpServerHandler>();
        }

        public override void ChannelInactive(IChannelHandlerContext context)
        {
            if (logger.IsEnabled(LogLevel.Information))
            {
                logger.LogInformation(context.Channel.Id.AsShortText());
            }
            jT1078HttpSessionManager.RemoveSessionByChannel(context.Channel);
            base.ChannelInactive(context);
        }

        protected override void ChannelRead0(IChannelHandlerContext ctx, object msg)
        {
            if (msg is IFullHttpRequest request)
            {             
                this.HandleHttpRequest(ctx, request);
            }
            else if (msg is WebSocketFrame frame)
            {
                this.HandleWebSocketFrame(ctx, frame);
            }
        }

        public override void ChannelReadComplete(IChannelHandlerContext context) => context.Flush();

        void HandleHttpRequest(IChannelHandlerContext ctx, IFullHttpRequest req)
        {
            // Handle a bad request.
            if (!req.Result.IsSuccess)
            {
                SendHttpResponse(ctx, req, new DefaultFullHttpResponse(Http11, BadRequest));
                return;
            }
            if ("/favicon.ico".Equals(req.Uri))
            {
                var res = new DefaultFullHttpResponse(Http11, NotFound);
                SendHttpResponse(ctx, req, res);
                return;
            }
            if (iJT1078Authorization.Authorization(req, out var principal))
            {
                if (req.Uri.StartsWith(WebsocketPath))
                {
                    // Handshake
                    var wsFactory = new WebSocketServerHandshakerFactory(GetWebSocketLocation(req), null, true, 5 * 1024 * 1024);
                    this.handshaker = wsFactory.NewHandshaker(req);
                    if (this.handshaker == null)
                    {
                        WebSocketServerHandshakerFactory.SendUnsupportedVersionResponse(ctx.Channel);
                    }
                    else
                    {
                        this.handshaker.HandshakeAsync(ctx.Channel, req);
                        jT1078HttpSessionManager.TryAdd(principal.Identity.Name, ctx.Channel);
                        httpMiddleware?.Next(ctx, req, principal);
                    }
                }
                else
                {
                    jT1078HttpSessionManager.TryAdd(principal.Identity.Name, ctx.Channel);
                    httpMiddleware?.Next(ctx, req, principal);
                }
            }
            else {
                SendHttpResponse(ctx, req, new DefaultFullHttpResponse(Http11, Unauthorized));
                return;
            }
        }

        void HandleWebSocketFrame(IChannelHandlerContext ctx, WebSocketFrame frame)
        {
            // Check for closing frame
            if (frame is CloseWebSocketFrame)
            {
                this.handshaker.CloseAsync(ctx.Channel, (CloseWebSocketFrame)frame.Retain());
                return;
            }
            if (frame is PingWebSocketFrame)
            {
                ctx.WriteAsync(new PongWebSocketFrame((IByteBuffer)frame.Content.Retain()));
                return;
            }
            if (frame is TextWebSocketFrame)
            {
                // Echo the frame
                ctx.WriteAsync(frame.Retain());
                return;
            }
            if (frame is BinaryWebSocketFrame)
            {
                // Echo the frame
                ctx.WriteAsync(frame.Retain());
            }
        }

        static void SendHttpResponse(IChannelHandlerContext ctx, IFullHttpRequest req, IFullHttpResponse res)
        {
            // Generate an error page if response getStatus code is not OK (200).
            if (res.Status.Code != 200)
            {
                res.Headers.Set(ServerEntity, ServerName);
                res.Headers.Set(DateEntity, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"));
                IByteBuffer buf = Unpooled.CopiedBuffer(Encoding.UTF8.GetBytes(res.Status.ToString()));
                res.Content.WriteBytes(buf);
                buf.Release();
                HttpUtil.SetContentLength(res, res.Content.ReadableBytes);
            }
            // Send the response and close the connection if necessary.
            Task task = ctx.Channel.WriteAndFlushAsync(res);
            if (!HttpUtil.IsKeepAlive(req) || res.Status.Code != 200)
            {
                task.ContinueWith((t, c) => ((IChannelHandlerContext)c).CloseAsync(), ctx, TaskContinuationOptions.ExecuteSynchronously);
            }
        }

        public override void ExceptionCaught(IChannelHandlerContext context, Exception exception)
        {
            logger.LogError(exception, context.Channel.Id.AsShortText());
            context.Channel.WriteAndFlushAsync(new DefaultFullHttpResponse(Http11, InternalServerError));
            jT1078HttpSessionManager.RemoveSessionByChannel(context.Channel);
            CloseAsync(context);
            base.ExceptionCaught(context, exception);
        }

        public override Task CloseAsync(IChannelHandlerContext context)
        {
            jT1078HttpSessionManager.RemoveSessionByChannel(context.Channel);
            return base.CloseAsync(context);
        }

        static string GetWebSocketLocation(IFullHttpRequest req)
        {
            bool result = req.Headers.TryGet(HttpHeaderNames.Host, out ICharSequence value);
            string location= value.ToString() + WebsocketPath;
            return "ws://" + location; 
        }
    }
}
