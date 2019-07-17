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
                while (true)
                {
                    try
                    {
                        foreach (var item in jT1078DataService.DataBlockingCollection.GetConsumingEnumerable())
                        {
                            //if (jT1078WebSocketSessionManager.GetAll().Count() > 0)
                            //{
                            //    Parallel.ForEach(jT1078WebSocketSessionManager.GetAll(), new ParallelOptions { MaxDegreeOfParallelism = 5 }, session =>
                            //    {
                            //        //if (item.Label3.SubpackageType == JT1078SubPackageType.分包处理时的第一个包)
                            //        //{
                            //        //    SubcontractKey.TryRemove(item.SIM, out _);

                            //        //    SubcontractKey.TryAdd(item.SIM, item.Bodies);
                            //        //}
                            //        //else if (item.Label3.SubpackageType == JT1078SubPackageType.分包处理时的中间包)
                            //        //{
                            //        //    if (SubcontractKey.TryGetValue(item.SIM, out var buffer))
                            //        //    {
                            //        //        SubcontractKey[item.SIM] = buffer.Concat(item.Bodies).ToArray();
                            //        //    }
                            //        //}
                            //        //else if (item.Label3.SubpackageType == JT1078SubPackageType.分包处理时的最后一个包)
                            //        //{
                            //        //    if (SubcontractKey.TryGetValue(item.SIM, out var buffer))
                            //        //    {
                            //        //        session.Channel.WriteAndFlushAsync(new BinaryWebSocketFrame(Unpooled.WrappedBuffer(buffer.Concat(item.Bodies).ToArray())));
                            //        //    }
                            //        //}
                            //        //else
                            //        //{
                            //                session.Channel.WriteAndFlushAsync(new BinaryWebSocketFrame(Unpooled.WrappedBuffer(item.Bodies)));
                            //        // }
                            //    });
                            //}

                            if (jT1078WebSocketSessionManager.GetAll().Count() > 0)
                            {
                                Parallel.ForEach(jT1078WebSocketSessionManager.GetAll(), new ParallelOptions { MaxDegreeOfParallelism = 5 }, session =>
                                {                             
                                    session.Channel.WriteAndFlushAsync(new BinaryWebSocketFrame(Unpooled.WrappedBuffer(item.Bodies)));
                                });
                            }
                        }
                    }
                    catch
                    {

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
