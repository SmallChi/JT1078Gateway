using DotNetty.Buffers;
using DotNetty.Codecs.Http.WebSockets;
using JT1078.DotNetty.Core.Session;
using JT1078.DotNetty.Core.Extensions;
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
using System.Diagnostics;
using System.IO.Pipes;
using Newtonsoft.Json;
using JT1078.DotNetty.TestHosting.JT1078WSFlv;
using JT1078.Flv;
using JT1078.Flv.H264;
using Microsoft.Extensions.Logging;

namespace JT1078.DotNetty.TestHosting
{
    /// <summary>
    /// 
    /// </summary>
    class JT1078WSFlvHostedService : IHostedService
    {
        private readonly JT1078HttpSessionManager jT1078HttpSessionManager;

        private ConcurrentDictionary<string, byte> exists = new ConcurrentDictionary<string, byte>();

        private readonly JT1078WSFlvDataService jT1078WSFlvDataService;
        private readonly FlvEncoder FlvEncoder = new FlvEncoder();
        private readonly ILogger logger;
        public JT1078WSFlvHostedService(
            ILoggerFactory  loggerFactory,
            JT1078WSFlvDataService jT1078WSFlvDataServic,
            JT1078HttpSessionManager jT1078HttpSessionManager)
        {
            logger = loggerFactory.CreateLogger("JT1078WSFlvHostedService");
            this.jT1078WSFlvDataService = jT1078WSFlvDataServic;
            this.jT1078HttpSessionManager = jT1078HttpSessionManager;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            Task.Run(() =>
            {
                try
                {
                    foreach (var item in jT1078WSFlvDataService.JT1078Packages.GetConsumingEnumerable())
                    {
                        var flv3 = FlvEncoder.CreateFlvFrame(item);
                        if (flv3 == null) continue;
                        if (jT1078HttpSessionManager.GetAll().Count() > 0)
                        {
                            foreach (var session in jT1078HttpSessionManager.GetAll())
                            {
                                if (!exists.ContainsKey(session.Channel.Id.AsShortText()))
                                {
                                    exists.TryAdd(session.Channel.Id.AsShortText(), 0);
                                    string key = item.GetKey();
                                    session.SendBinaryWebSocketAsync(FlvEncoder.GetFirstFlvFrame(key, flv3));
                                }
                                session.SendBinaryWebSocketAsync(flv3);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
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
