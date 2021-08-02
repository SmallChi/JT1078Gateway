using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Channels;

namespace JT1078.Gateway.InMemoryMQ
{
    public class JT1078MsgChannel
    {
        public Channel<(string, byte[])> Channel { get;}

        public JT1078MsgChannel()
        {
            Channel = System.Threading.Channels.Channel.CreateUnbounded<(string, byte[])>(new UnboundedChannelOptions { 
                 SingleWriter=true,
            });
        }
    }
}
