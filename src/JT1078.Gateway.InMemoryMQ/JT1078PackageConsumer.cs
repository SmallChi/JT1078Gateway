using JT1078.Gateway.Abstractions;
using JT1078.Protocol;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace JT1078.Gateway.InMemoryMQ
{
    public class JT1078PackageConsumer: IJT1078PackageConsumer
    {
        private JT1078MsgChannel Channel;
        private readonly ILogger logger;

        public JT1078PackageConsumer(ILoggerFactory loggerFactory,JT1078MsgChannel channel)
        {
            Channel = channel;
            logger = loggerFactory.CreateLogger<JT1078PackageConsumer>();
        }

        public CancellationTokenSource Cts { get; } = new CancellationTokenSource();

        public string TopicName { get; } = "JT1078Package";

        public void Dispose()
        {

        }

        public void OnMessage(Action<(string TerminalNo, JT1078Package Data)> callback)
        {
            Task.Run(async() => 
            {
                while (!Cts.IsCancellationRequested)
                {
                    var reader = await Channel.Channel.Reader.ReadAsync(Cts.Token);
                    if (logger.IsEnabled(LogLevel.Trace))
                    {
                        logger.LogTrace(JsonSerializer.Serialize(reader.Item2));
                    }
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
