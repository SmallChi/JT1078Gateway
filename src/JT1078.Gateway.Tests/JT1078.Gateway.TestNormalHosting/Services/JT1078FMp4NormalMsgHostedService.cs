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
        private H264Decoder H264Decoder;
        public JT1078FMp4NormalMsgHostedService(
            MessageDispatchDataService messageDispatchDataService,
            IMemoryCache memoryCache,
            ILoggerFactory loggerFactory,
            FMp4Encoder fM4Encoder,
            H264Decoder h264Decoder,
            JT1078HttpSessionManager httpSessionManager)
        {
            Logger = loggerFactory.CreateLogger<JT1078FMp4NormalMsgHostedService>();
            HttpSessionManager = httpSessionManager;
            FM4Encoder = fM4Encoder;
            H264Decoder= h264Decoder;
            this.memoryCache = memoryCache;
            this.messageDispatchDataService = messageDispatchDataService;
            //todo:定时清理
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
                    var nalus = H264Decoder.ParseNALU(data);
                    string key = $"{data.GetKey()}_{ikey}";
                    if (data.Label3.DataType == Protocol.Enums.JT1078DataType.视频I帧)
                    {
                        var moovBuffer = FM4Encoder.EncoderMoovBox(
                            nalus.FirstOrDefault(f => f.NALUHeader.NalUnitType == NalUnitType.SPS),
                            nalus.FirstOrDefault(f => f.NALUHeader.NalUnitType == NalUnitType.PPS));
                        memoryCache.Set(key, moovBuffer);
                    }
                    //查找第一帧为I帧，否则不推送
                    if (memoryCache.TryGetValue(key, out byte[] moov))
                    {
                        var httpSessions = HttpSessionManager.GetAllBySimAndChannelNo(data.SIM.TrimStart('0'), data.LogicChannelNumber);
                        var firstHttpSessions = httpSessions.Where(w => !w.FirstSend).ToList();
                        if (firstHttpSessions.Count > 0)
                        {
                            try
                            {
                                try
                                {
                                    var ftyp = FM4Encoder.EncoderFtypBox();
                                    foreach (var session in firstHttpSessions)
                                    {
                                        HttpSessionManager.SendAVData(session, ftyp, true);
                                        HttpSessionManager.SendAVData(session, moov, false);
                                    }
                                }
                                catch (Exception ex)
                                {
                                    Logger.LogError(ex, $"{data.SIM},{true},{data.SN},{data.LogicChannelNumber},{data.Label3.DataType.ToString()},{data.Label3.SubpackageType.ToString()},{data.Bodies.ToHexString()}");
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
                                var firstNALU = nalus.FirstOrDefault();
                                if (firstNALU == null)
                                {
                                    continue;
                                }
                                if(!avFrameDict.TryGetValue(firstNALU.GetKey(),out List<H264NALU> cacheNALU))
                                {
                                    cacheNALU = new List<H264NALU>();
                                    avFrameDict.TryAdd(firstNALU.GetKey(), cacheNALU);
                                }
                                foreach (var nalu in nalus)
                                {
                                    if (nalu.Slice)
                                    {
                                        //H264 NALU slice first_mb_in_slice
                                        cacheNALU.Add(nalu);
                                    }
                                    else
                                    {
                                        if (cacheNALU.Count > 0)
                                        {
                                            foreach (var session in otherHttpSessions)
                                            {
                                                var fmp4VideoBuffer = FM4Encoder.EncoderOtherVideoBox(cacheNALU);
                                                HttpSessionManager.SendAVData(session, fmp4VideoBuffer, false);
                                            }
                                            cacheNALU.Clear();
                                        }
                                        cacheNALU.Add(nalu);
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                Logger.LogError(ex, $"{data.SIM},{false},{data.SN},{data.LogicChannelNumber},{data.Label3.DataType.ToString()},{data.Label3.SubpackageType.ToString()},{data.Bodies.ToHexString()}");
                            }
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
