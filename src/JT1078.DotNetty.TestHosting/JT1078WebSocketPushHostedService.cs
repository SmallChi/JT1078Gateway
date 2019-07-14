using DotNetty.Buffers;
using DotNetty.Codecs.Http.WebSockets;
using JT1078.DotNetty.Core.Session;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using JT1078.Protocol;

namespace JT1078.DotNetty.TestHosting
{
    class JT1078WebSocketPushHostedService : IHostedService
    {
        JT1078WebSocketSessionManager jT1078WebSocketSessionManager;

        public JT1078WebSocketPushHostedService(JT1078WebSocketSessionManager jT1078WebSocketSessionManager)
        {
            this.jT1078WebSocketSessionManager = jT1078WebSocketSessionManager;
        }
        public Task StartAsync(CancellationToken cancellationToken)
        {
            var lines = File.ReadAllLines(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "2019-07-12.log"));

            Task.Run(() =>
            {
                while (true)
                {
                    var session = jT1078WebSocketSessionManager.GetAll().FirstOrDefault();
                    if (session != null)
                    {
                        for (int i = 0; i < lines.Length; i++)
                        {
                            var package = JT1078Serializer.Deserialize(lines[i].Split(',')[6].ToHexBytes());
                            session.Channel.WriteAndFlushAsync(new BinaryWebSocketFrame(Unpooled.WrappedBuffer(package.Bodies)));
                        }
                    }
                    Thread.Sleep(10000);
                }
            });
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
