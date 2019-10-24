using DotNetty.Buffers;
using DotNetty.Codecs.Http.WebSockets;
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
using JT1078.Flv;
using Microsoft.Extensions.Logging;
using JT1078.Gateway.Session;
using JT1078.Gateway.Extensions;

namespace JT1078.Gateway.SimpleServer.JT1078WSFlv
{
    /// <summary>
    /// 
    /// </summary>
    class JT1078WSFlvHostedService : IHostedService
    {
        private readonly JT1078HttpSessionManager jT1078HttpSessionManager;

        private ConcurrentDictionary<string, byte> exists = new ConcurrentDictionary<string, byte>();

        private readonly JT1078WSFlvDataService jT1078WSFlvDataService;
        private readonly FlvEncoder FlvEncoder;
        private readonly ILogger logger;
        private readonly ILogger flvEncodingLogger;
        public JT1078WSFlvHostedService(
            ILoggerFactory  loggerFactory,
            JT1078WSFlvDataService jT1078WSFlvDataServic,
            JT1078HttpSessionManager jT1078HttpSessionManager)
        {
            logger = loggerFactory.CreateLogger("JT1078WSFlvHostedService");
            flvEncodingLogger = loggerFactory.CreateLogger("FlvEncoding");
            this.jT1078WSFlvDataService = jT1078WSFlvDataServic;
            this.jT1078HttpSessionManager = jT1078HttpSessionManager;
            FlvEncoder = new FlvEncoder(loggerFactory);
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            Task.Run(() =>
            {
                try
                {
                    Stopwatch stopwatch = new Stopwatch();
                    foreach (var item in jT1078WSFlvDataService.JT1078Packages.GetConsumingEnumerable())
                    {
                        stopwatch.Start();
                        var flv3 = FlvEncoder.CreateFlvFrame(item);
                        stopwatch.Stop();
                        if(flvEncodingLogger.IsEnabled(LogLevel.Debug))
                        {
                            long times = stopwatch.ElapsedMilliseconds;
                            flvEncodingLogger.LogDebug($"flv encoding {times.ToString()}ms");
                        }
                        stopwatch.Reset();
                        if (flv3 == null) continue;
                        if (jT1078HttpSessionManager.GetAll().Count() > 0)
                        {
                            foreach (var session in jT1078HttpSessionManager.GetAll())
                            {
                                if (!exists.ContainsKey(session.Channel.Id.AsShortText()))
                                {
                                    exists.TryAdd(session.Channel.Id.AsShortText(), 0);
                                    string key = item.GetKey();
                                    //ws-flv
                                    //session.SendBinaryWebSocketAsync(FlvEncoder.GetFirstFlvFrame(key, flv3));
                                    //http-flv
                                    var buffer = FlvEncoder.GetFirstFlvFrame(key, flv3);
                                    if (buffer != null)
                                    {
                                        flvEncodingLogger.LogDebug(JsonConvert.SerializeObject(buffer));
                                        session.SendHttpFirstChunkAsync(buffer);
                                    }
                                    continue;
                                }
                                //ws-flv
                                //session.SendBinaryWebSocketAsync(flv3);
                                //http-flv
                                session.SendHttpOtherChunkAsync(flv3);
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
