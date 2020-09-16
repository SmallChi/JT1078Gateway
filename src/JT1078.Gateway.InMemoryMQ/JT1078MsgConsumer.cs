using JT1078.Gateway.Abstractions;
using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace JT1078.Gateway.InMemoryMQ
{
    public class JT1078MsgConsumer : IJT1078MsgConsumer
    {
        private JT1078MsgChannel Channel;

        public JT1078MsgConsumer(JT1078MsgChannel channel)
        {
            Channel = channel;
        }

        public CancellationTokenSource Cts { get; } = new CancellationTokenSource();

        public string TopicName { get; } = "JT1078Package";

        public void Dispose()
        {

        }

        public void OnMessage(Action<(string SIM, byte[] Data)> callback)
        {
            Task.Run(async() => 
            {
                while (!Cts.IsCancellationRequested)
                {
                    var reader = await Channel.Channel.Reader.ReadAsync(Cts.Token);
                    callback(reader);
                }
            }, Cts.Token);
        }

        public void Subscribe()
        {
            
        }

        public void Unsubscribe()
        {

        }
    }
}
