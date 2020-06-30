using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Channels;

namespace JT1078.Gateway.InMemoryMQ
{
    public class JT1078MsgChannel
    {
        public Channel<(string, JT1078.Protocol.JT1078Package)> Channel { get;}

        public JT1078MsgChannel()
        {
            Channel = System.Threading.Channels.Channel.CreateUnbounded<(string, JT1078.Protocol.JT1078Package)>();
        }
    }
}
