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
using System.Collections.Concurrent;
using JT1078.Protocol.Enums;

namespace JT1078.DotNetty.TestHosting
{
    class JT1078WebSocketPushHostedService : IHostedService
    {
        JT1078WebSocketSessionManager jT1078WebSocketSessionManager;
        private readonly JT1078DataService jT1078DataService;
        private readonly ConcurrentDictionary<string, byte[]> SubcontractKey;

        public JT1078WebSocketPushHostedService(
            JT1078DataService jT1078DataService,
            JT1078WebSocketSessionManager jT1078WebSocketSessionManager)
        {
            SubcontractKey = new ConcurrentDictionary<string, byte[]>(StringComparer.OrdinalIgnoreCase);
            this.jT1078DataService = jT1078DataService;
            this.jT1078WebSocketSessionManager = jT1078WebSocketSessionManager;
        }
        public Task StartAsync(CancellationToken cancellationToken)
        {
            Task.Run(() =>
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    try
                    {
                        foreach (var item in jT1078DataService.DataBlockingCollection.GetConsumingEnumerable(cancellationToken))
                        {
                            Parallel.ForEach(jT1078WebSocketSessionManager.GetAll(), new ParallelOptions { MaxDegreeOfParallelism = 5 }, session =>
                            {
                                 session.Channel.WriteAndFlushAsync(new BinaryWebSocketFrame(Unpooled.WrappedBuffer(item)));
                            });
                        }
                    }
                    catch
                    {

                    }
                }
            }, cancellationToken);
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
