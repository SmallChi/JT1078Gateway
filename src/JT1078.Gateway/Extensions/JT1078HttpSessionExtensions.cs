using DotNetty.Buffers;
using DotNetty.Codecs.Http;
using DotNetty.Codecs.Http.WebSockets;
using DotNetty.Common.Utilities;
using JT1078.Gateway.Metadata;
using System;
using System.Collections.Generic;
using System.Text;

namespace JT1078.Gateway.Extensions
{
    public static class JT1078HttpSessionExtensions
    {
        private static readonly AsciiString ServerName = AsciiString.Cached("JT1078Netty");
        private static readonly AsciiString DateEntity = HttpHeaderNames.Date;
        private static readonly AsciiString ServerEntity = HttpHeaderNames.Server;
        public static void SendBinaryWebSocketAsync(this JT1078HttpSession session,byte[] data)
        {
            session.Channel.WriteAndFlushAsync(new BinaryWebSocketFrame(Unpooled.WrappedBuffer(data)));
        }
        public static void SendHttpFirstChunkAsync(this JT1078HttpSession session, byte[] data)
        {
            DefaultHttpResponse firstRes = new DefaultHttpResponse(HttpVersion.Http11, HttpResponseStatus.OK);
            firstRes.Headers.Set(ServerEntity, ServerName);
            firstRes.Headers.Set(DateEntity, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"));
            firstRes.Headers.Set(HttpHeaderNames.ContentType, (AsciiString)"video/x-flv");
            HttpUtil.SetTransferEncodingChunked(firstRes, true);
            session.Channel.WriteAsync(firstRes);
            session.Channel.WriteAndFlushAsync(Unpooled.CopiedBuffer(data));
        }
        public static void SendHttpOtherChunkAsync(this JT1078HttpSession session, byte[] data)
        {
            session.Channel.WriteAndFlushAsync(Unpooled.CopiedBuffer(data));
        }
    }
}
