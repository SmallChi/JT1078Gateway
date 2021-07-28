using JT1078.Gateway.Sessions;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Caching.Memory;
using JT1078.Protocol;
using System.Text.Json;
using JT1078.Protocol.H264;
using System.Collections.Concurrent;
using JT1078.FMp4;
using JT1078.Protocol.Extensions;
using System.IO;

namespace JT1078.Gateway.TestNormalHosting.Services
{
    public class JT1078FMp4NormalMsgHostedService : BackgroundService
    {
        private JT1078HttpSessionManager HttpSessionManager;
        private FMp4Encoder FM4Encoder;
        private ILogger Logger;
        private IMemoryCache memoryCache;
        private const string ikey = "IFMp4KEY";
        private MessageDispatchDataService messageDispatchDataService;
        private ConcurrentDictionary<string, List<H264NALU>> avFrameDict;

        public JT1078FMp4NormalMsgHostedService(
            MessageDispatchDataService messageDispatchDataService,
            IMemoryCache memoryCache,
            ILoggerFactory loggerFactory,
            FMp4Encoder fM4Encoder,
            JT1078HttpSessionManager httpSessionManager)
        {
            Logger = loggerFactory.CreateLogger<JT1078FMp4NormalMsgHostedService>();
            HttpSessionManager = httpSessionManager;
            FM4Encoder = fM4Encoder;
            this.memoryCache = memoryCache;
            this.messageDispatchDataService = messageDispatchDataService;
            avFrameDict = new ConcurrentDictionary<string, List<H264NALU>>();
        }
        protected async override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                var data = await messageDispatchDataService.FMp4Channel.Reader.ReadAsync();

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
                        try
                        {
                            if (memoryCache.TryGetValue(key, out JT1078Package idata))
                            {
                                try
                                {
                                    foreach (var session in firstHttpSessions)
                                    {
                                        var fmp4VideoBuffer = FM4Encoder.EncoderVideo(idata, session.FMp4EncoderInfo, true);
                                        HttpSessionManager.SendAVData(session, fmp4VideoBuffer, true);             
                                    }
                                }
                                catch (Exception ex)
                                {
                                    Logger.LogError(ex, $"{data.SIM},{true},{data.SN},{data.LogicChannelNumber},{data.Label3.DataType.ToString()},{data.Label3.SubpackageType.ToString()},{data.Bodies.ToHexString()}");
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            Logger.LogError(ex, $"{data.SIM},{true},{data.SN},{data.LogicChannelNumber},{data.Label3.DataType.ToString()},{data.Label3.SubpackageType.ToString()},{data.Bodies.ToHexString()}");
                        }
                    }
                    var otherHttpSessions = httpSessions.Where(w => w.FirstSend).ToList();
                    if (otherHttpSessions.Count > 0)
                    {
                        try
                        {
                            foreach (var session in otherHttpSessions)
                            {
                                var fmp4VideoBuffer = FM4Encoder.EncoderVideo(data, session.FMp4EncoderInfo, false);
                                HttpSessionManager.SendAVData(session, fmp4VideoBuffer, false);
                            }
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
