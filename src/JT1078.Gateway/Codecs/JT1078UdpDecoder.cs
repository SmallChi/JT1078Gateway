using DotNetty.Buffers;
using DotNetty.Codecs;
using DotNetty.Transport.Channels;
using System.Collections.Generic;
using DotNetty.Transport.Channels.Sockets;
using JT1078.Gateway.Metadata;

namespace JT1078.Gateway.Codecs
{
    public class JT1078UdpDecoder : MessageToMessageDecoder<DatagramPacket>
    {
        protected override void Decode(IChannelHandlerContext context, DatagramPacket message, List<object> output)
        {
            if (!message.Content.IsReadable()) return;
            IByteBuffer byteBuffer = message.Content;
            byte[] buffer = new byte[byteBuffer.ReadableBytes];
            byteBuffer.ReadBytes(buffer);
            output.Add(new JT1078UdpPackage(buffer, message.Sender));
        }
    }
}
