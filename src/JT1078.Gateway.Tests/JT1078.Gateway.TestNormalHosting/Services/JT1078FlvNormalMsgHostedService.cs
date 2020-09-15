using JT1078.Gateway.Abstractions;
using JT1078.Gateway.Sessions;
using JT1078.Flv;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.Extensions.Logging;
using JT1078.Flv.Extensions;
using Microsoft.Extensions.Caching.Memory;
using JT1078.Protocol;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace JT1078.Gateway.TestNormalHosting.Services
{
    public class JT1078FlvNormalMsgHostedService : BackgroundService
    {
        private IJT1078PackageConsumer PackageConsumer;
        private JT1078HttpSessionManager HttpSessionManager;
        private FlvEncoder FlvEncoder;
        private ILogger Logger;
        private IMemoryCache memoryCache;
        private const string ikey = "IKEY";

        public JT1078FlvNormalMsgHostedService(
            IMemoryCache memoryCache,
            ILoggerFactory loggerFactory,
            FlvEncoder flvEncoder,
            JT1078HttpSessionManager httpSessionManager,
            IJT1078PackageConsumer packageConsumer)
        {
            Logger = loggerFactory.CreateLogger<JT1078FlvNormalMsgHostedService>();
            PackageConsumer = packageConsumer;
            HttpSessionManager = httpSessionManager;
            FlvEncoder = flvEncoder;
            this.memoryCache = memoryCache;
        }
        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            PackageConsumer.OnMessage((Message) =>
            {
                try
                {
                    if (Logger.IsEnabled(LogLevel.Debug))
                    {
                        Logger.LogDebug(JsonSerializer.Serialize(HttpSessionManager.GetAll()));
                        Logger.LogDebug($"{Message.Data.SIM},{Message.Data.SN},{Message.Data.LogicChannelNumber},{Message.Data.Label3.DataType.ToString()},{Message.Data.Label3.SubpackageType.ToString()},{Message.Data.Bodies.ToHexString()}");
                    }
                    var merge = JT1078.Protocol.JT1078Serializer.Merge(Message.Data);
                    string key = $"{Message.Data.GetKey()}_{ikey}";
                    if (merge != null)
                    {
                        if (merge.Label3.DataType == Protocol.Enums.JT1078DataType.视频I帧)
                        {
                            memoryCache.Set(key, merge);
                        }
                        var httpSessions = HttpSessionManager.GetAllBySimAndChannelNo(Message.Data.SIM.TrimStart('0'), Message.Data.LogicChannelNumber);
                        var firstHttpSessions = httpSessions.Where(w => !w.FirstSend).ToList();
                        if (firstHttpSessions.Count > 0)
                        {
                            if (memoryCache.TryGetValue(key, out JT1078Package idata))
                            {
                                try
                                {
                                    var flvVideoBuffer = FlvEncoder.EncoderVideoTag(idata, true);
                                    foreach (var session in firstHttpSessions)
                                    {
                                        HttpSessionManager.SendAVData(session, flvVideoBuffer, true);
                                    }
                                }
                                catch (Exception ex)
                                {
                                    Logger.LogError(ex, $"{Message.Data.SIM},{true},{Message.Data.SN},{Message.Data.LogicChannelNumber},{Message.Data.Label3.DataType.ToString()},{Message.Data.Label3.SubpackageType.ToString()},{Message.Data.Bodies.ToHexString()}");
                                }
                            }
                        }
                        var otherHttpSessions = httpSessions.Where(w => w.FirstSend).ToList();
                        if (otherHttpSessions.Count > 0)
                        {
                            try
                            {
                                var flvVideoBuffer = FlvEncoder.EncoderVideoTag(merge, false);
                                foreach (var session in otherHttpSessions)
                                {
                                    HttpSessionManager.SendAVData(session, flvVideoBuffer, false);
                                }
                            }
                            catch (Exception ex)
                            {
                                Logger.LogError(ex, $"{Message.Data.SIM},{false},{Message.Data.SN},{Message.Data.LogicChannelNumber},{Message.Data.Label3.DataType.ToString()},{Message.Data.Label3.SubpackageType.ToString()},{Message.Data.Bodies.ToHexString()}");
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, $"{Message.Data.SIM},{Message.Data.SN},{Message.Data.LogicChannelNumber},{Message.Data.Label3.DataType.ToString()},{Message.Data.Label3.SubpackageType.ToString()},{Message.Data.Bodies.ToHexString()}");
                }
            });
            return Task.CompletedTask;
        }
    }
}
