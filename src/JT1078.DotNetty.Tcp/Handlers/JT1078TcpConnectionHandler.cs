using DotNetty.Handlers.Timeout;
using DotNetty.Transport.Channels;
using JT1078.DotNetty.Core.Session;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace JT1078.DotNetty.Tcp.Handlers
{
    /// <summary>
    /// JT1078服务通道处理程序
    /// </summary>
    internal class JT1078TcpConnectionHandler : ChannelHandlerAdapter
    {
        private readonly ILogger<JT1078TcpConnectionHandler> logger;

        private readonly JT1078TcpSessionManager SessionManager;

        public JT1078TcpConnectionHandler(
            JT1078TcpSessionManager sessionManager,
            ILoggerFactory loggerFactory)
        {
            this.SessionManager = sessionManager;
            logger = loggerFactory.CreateLogger<JT1078TcpConnectionHandler>();
        }

        /// <summary>
        /// 通道激活
        /// </summary>
        /// <param name="context"></param>
        public override void ChannelActive(IChannelHandlerContext context)
        {
            string channelId = context.Channel.Id.AsShortText();
            if (logger.IsEnabled(LogLevel.Debug))
                logger.LogDebug($"<<<{ channelId } Successful client connection to server.");
            base.ChannelActive(context);
        }

        /// <summary>
        /// 设备主动断开
        /// </summary>
        /// <param name="context"></param>
        public override void ChannelInactive(IChannelHandlerContext context)
        {
            string channelId = context.Channel.Id.AsShortText();
            if (logger.IsEnabled(LogLevel.Debug))
                logger.LogDebug($">>>{ channelId } The client disconnects from the server.");
            SessionManager.RemoveSessionByChannel(context.Channel);
            base.ChannelInactive(context);
        }

        /// <summary>
        /// 服务器主动断开
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public override Task CloseAsync(IChannelHandlerContext context)
        {
            string channelId = context.Channel.Id.AsShortText();
            if (logger.IsEnabled(LogLevel.Debug))
                logger.LogDebug($"<<<{ channelId } The server disconnects from the client.");
            SessionManager.RemoveSessionByChannel(context.Channel);
            return base.CloseAsync(context);
        }

        public override void ChannelReadComplete(IChannelHandlerContext context)=> context.Flush();

        /// <summary>
        /// 超时策略
        /// </summary>
        /// <param name="context"></param>
        /// <param name="evt"></param>
        public override void UserEventTriggered(IChannelHandlerContext context, object evt)
        {
            IdleStateEvent idleStateEvent = evt as IdleStateEvent;
            if (idleStateEvent != null)
            {
                if(idleStateEvent.State== IdleState.ReaderIdle)
                {
                    string channelId = context.Channel.Id.AsShortText();
                    logger.LogInformation($"{idleStateEvent.State.ToString()}>>>{channelId}");
                    // 由于808是设备发心跳，如果很久没有上报数据，那么就由服务器主动关闭连接。
                    SessionManager.RemoveSessionByChannel(context.Channel);
                    context.CloseAsync();
                }
            }
            base.UserEventTriggered(context, evt);
        }

        public override void ExceptionCaught(IChannelHandlerContext context, Exception exception)
        {
            string channelId = context.Channel.Id.AsShortText();
            logger.LogError(exception,$"{channelId} {exception.Message}" );
            SessionManager.RemoveSessionByChannel(context.Channel);
            context.CloseAsync();
        }
    }
}

