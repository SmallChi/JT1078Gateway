using JT1078.Gateway.Abstractions;
using JT1078.Protocol;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace JT1078.Gateway.InMemoryMQ
{
    public class JT1078MsgProducer : IJT1078MsgProducer
    {
        public string TopicName { get; }= "JT1078Package";

        private JT1078MsgChannel Channel;

        public JT1078MsgProducer(JT1078MsgChannel channel)
        {
            Channel = channel;
        }

        public void Dispose()
        {
            
        }

        public async ValueTask ProduceAsync(string sim, byte[] data)
        {
            await Channel.Channel.Writer.WriteAsync((sim, data));
        }
    }
}
