using DotNetty.Buffers;
using DotNetty.Transport.Channels;
using System;
using Microsoft.Extensions.Logging;
using JT1078.DotNetty.Core.Metadata;
using JT1078.DotNetty.Core.Session;
using JT1078.DotNetty.Core.Services;
using JT1078.DotNetty.Core.Enums;
using JT1078.Protocol;
using JT1078.DotNetty.Core.Interfaces;

namespace JT1078.DotNetty.Udp.Handlers
{
    /// <summary>
    /// JT1078 Udp服务端处理程序
    /// </summary>
    internal class JT1078UdpServerHandler : SimpleChannelInboundHandler<JT1078UdpPackage>
    {
        private readonly ILogger<JT1078UdpServerHandler> logger;

        private readonly JT1078UdpSessionManager SessionManager;

        private readonly JT1078AtomicCounterService AtomicCounterService;

        private readonly IJT1078UdpMessageHandlers handlers;
        public JT1078UdpServerHandler(
            ILoggerFactory loggerFactory,
            JT1078AtomicCounterServiceFactory  atomicCounterServiceFactory,
            IJT1078UdpMessageHandlers handlers,
            JT1078UdpSessionManager sessionManager)
        {
            this.AtomicCounterService = atomicCounterServiceFactory.Create(JT1078TransportProtocolType.udp);
            this.SessionManager = sessionManager;
            logger = loggerFactory.CreateLogger<JT1078UdpServerHandler>();
            this.handlers = handlers;
        }

        protected override void ChannelRead0(IChannelHandlerContext ctx, JT1078UdpPackage msg)
        {
            try
            {
                if (logger.IsEnabled(LogLevel.Trace))
                {
                    logger.LogTrace("accept package success count<<<" + AtomicCounterService.MsgSuccessCount.ToString());
                    logger.LogTrace("accept msg <<< " + ByteBufferUtil.HexDump(msg.Buffer));
                }
                JT1078Package package = JT1078Serializer.Deserialize(msg.Buffer);
                AtomicCounterService.MsgSuccessIncrement();
                SessionManager.TryAdd(ctx.Channel, msg.Sender, package.SIM);
                handlers.Processor(new JT1078Request(package, msg.Buffer));
                if (logger.IsEnabled(LogLevel.Debug))
                {
                    logger.LogDebug("accept package success count<<<" + AtomicCounterService.MsgSuccessCount.ToString());
                }
            }
            catch (Exception ex)
            {
                AtomicCounterService.MsgFailIncrement();
                if (logger.IsEnabled(LogLevel.Error))
                {
                    logger.LogError("accept package fail count<<<" + AtomicCounterService.MsgFailCount.ToString());
                    logger.LogError(ex, "accept msg<<<" + ByteBufferUtil.HexDump(msg.Buffer));
                }
            }
        }

        public override void ChannelReadComplete(IChannelHandlerContext context) => context.Flush();

    }
}
