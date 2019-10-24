using DotNetty.Buffers;
using DotNetty.Codecs;
using System.Collections.Generic;
using DotNetty.Transport.Channels;
using JT1078.Protocol;
using System;

namespace JT1078.Gateway.Codecs
{
    public class JT1078TcpDecoder : ByteToMessageDecoder
    {
        protected override void Decode(IChannelHandlerContext context, IByteBuffer input, List<object> output)
        {
            byte[] buffer = new byte[input.Capacity+4];
            input.ReadBytes(buffer, 4, input.Capacity);
            Array.Copy(JT1078Package.FH_Bytes, 0,buffer, 0, 4);
            output.Add(buffer);
        }
    }
}
