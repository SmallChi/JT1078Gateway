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
using JT1078.FMp4;

namespace JT1078.Gateway.TestNormalHosting.Services
{
    public class JT1078FMp4NormalMsgHostedService : BackgroundService
    {
        private IJT1078MsgConsumer JT1078MsgConsumer;
        private JT1078HttpSessionManager HttpSessionManager;
        private FMp4Encoder FM4Encoder;
        private ILogger Logger;
        private IMemoryCache memoryCache;
        private const string ikey = "IKEY";
        private MessageDispatchDataService messageDispatchDataService;

        public JT1078FMp4NormalMsgHostedService(
            MessageDispatchDataService messageDispatchDataService,
            IMemoryCache memoryCache,
            ILoggerFactory loggerFactory,
            FMp4Encoder fM4Encoder,
            JT1078HttpSessionManager httpSessionManager,
            IJT1078MsgConsumer msgConsumer)
        {
            Logger = loggerFactory.CreateLogger<JT1078FMp4NormalMsgHostedService>();
            JT1078MsgConsumer = msgConsumer;
            HttpSessionManager = httpSessionManager;
            FM4Encoder = fM4Encoder;
            this.memoryCache = memoryCache;
            this.messageDispatchDataService = messageDispatchDataService;
        }
        protected async override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                var data = await messageDispatchDataService.FlvChannel.Reader.ReadAsync();
                try
                {      
                    if (Logger.IsEnabled(LogLevel.Debug))
                    {
                        Logger.LogDebug(JsonSerializer.Serialize(HttpSessionManager.GetAll()));
                        Logger.LogDebug($"{data.SIM},{data.SN},{data.LogicChannelNumber},{data.Label3.DataType.ToString()},{data.Label3.SubpackageType.ToString()},{data.Bodies.ToHexString()}");
                    }
                    string key = $"{data.GetKey()}_{ikey}";
                    if (data.Label3.DataType == Protocol.Enums.JT1078DataType.视频I帧)
                    {
                        memoryCache.Set(key, data);
                    }
                    var httpSessions = HttpSessionManager.GetAllBySimAndChannelNo(data.SIM.TrimStart('0'), data.LogicChannelNumber);
                    var firstHttpSessions = httpSessions.Where(w => !w.FirstSend).ToList();
                    if (firstHttpSessions.Count > 0)
                    {
                        if (memoryCache.TryGetValue(key, out JT1078Package idata))
                        {
                            try
                            {
                                
                            }
                            catch (Exception ex)
                            {
                                Logger.LogError(ex, $"{data.SIM},{true},{data.SN},{data.LogicChannelNumber},{data.Label3.DataType.ToString()},{data.Label3.SubpackageType.ToString()},{data.Bodies.ToHexString()}");
                            }
                        }
                    }
                    var otherHttpSessions = httpSessions.Where(w => w.FirstSend).ToList();
                    if (otherHttpSessions.Count > 0)
                    {
                        try
                        {
                           
                        }
                        catch (Exception ex)
                        {
                            Logger.LogError(ex, $"{data.SIM},{false},{data.SN},{data.LogicChannelNumber},{data.Label3.DataType.ToString()},{data.Label3.SubpackageType.ToString()},{data.Bodies.ToHexString()}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, $"{data.SIM},{data.SN},{data.LogicChannelNumber},{data.Label3.DataType.ToString()},{data.Label3.SubpackageType.ToString()},{data.Bodies.ToHexString()}");
                }
            }
            await Task.CompletedTask;
        }
    }
}
