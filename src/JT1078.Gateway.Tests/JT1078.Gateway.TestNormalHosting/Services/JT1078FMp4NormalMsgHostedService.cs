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
using JT1078.Protocol.H264;
using System.Collections.Concurrent;

namespace JT1078.Gateway.TestNormalHosting.Services
{
    public class JT1078FMp4NormalMsgHostedService : BackgroundService
    {
        private JT1078HttpSessionManager HttpSessionManager;
        private FMp4Encoder FM4Encoder;
        private readonly H264Decoder H264Decoder;
        private ILogger Logger;
        private IMemoryCache memoryCache;
        private const string ikey = "IFMp4KEY";
        private MessageDispatchDataService messageDispatchDataService;
        private ConcurrentDictionary<string, List<H264NALU>> avFrameDict;

        public JT1078FMp4NormalMsgHostedService(
            MessageDispatchDataService messageDispatchDataService,
            IMemoryCache memoryCache,
            ILoggerFactory loggerFactory,
            H264Decoder h264Decoder,
            FMp4Encoder fM4Encoder,
            JT1078HttpSessionManager httpSessionManager)
        {
            Logger = loggerFactory.CreateLogger<JT1078FMp4NormalMsgHostedService>();
            HttpSessionManager = httpSessionManager;
            FM4Encoder = fM4Encoder;
            H264Decoder = h264Decoder;
            this.memoryCache = memoryCache;
            this.messageDispatchDataService = messageDispatchDataService;
            avFrameDict = new ConcurrentDictionary<string, List<H264NALU>>();
        }
        protected async override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                var data = await messageDispatchDataService.FlvChannel.Reader.ReadAsync();
                try
                {
                    var nalus = H264Decoder.ParseNALU(data);
                    if (Logger.IsEnabled(LogLevel.Debug))
                    {
                        Logger.LogDebug(JsonSerializer.Serialize(HttpSessionManager.GetAll()));
                        Logger.LogDebug($"{data.SIM},{data.SN},{data.LogicChannelNumber},{data.Label3.DataType.ToString()},{data.Label3.SubpackageType.ToString()},{data.Bodies.ToHexString()}");
                    }
                    string key = $"{data.GetKey()}_{ikey}";
                    if (data.Label3.DataType == Protocol.Enums.JT1078DataType.视频I帧)
                    {
                        var moov = FM4Encoder.EncoderMoovBox(nalus.FirstOrDefault(f => f.NALUHeader.NalUnitType == NalUnitType.SPS),
                            nalus.FirstOrDefault(f => f.NALUHeader.NalUnitType == NalUnitType.PPS));
                        memoryCache.Set(key, moov);
                    }
                    var httpSessions = HttpSessionManager.GetAllBySimAndChannelNo(data.SIM.TrimStart('0'), data.LogicChannelNumber);
                    var firstHttpSessions = httpSessions.Where(w => !w.FirstSend).ToList();
                    if (firstHttpSessions.Count > 0)
                    {
                        try
                        {
                            var flvVideoBuffer = FM4Encoder.EncoderFtypBox();
                            memoryCache.TryGetValue(key, out byte[] moovBuffer);
                            foreach (var session in firstHttpSessions)
                            {
                                HttpSessionManager.SendAVData(session, flvVideoBuffer, true);
                                if (moovBuffer != null)
                                {
                                    HttpSessionManager.SendAVData(session, moovBuffer, false);
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
                            if(!avFrameDict.TryGetValue(key, out List<H264NALU> frames))
                            {
                                frames = new List<H264NALU>();
                                avFrameDict.TryAdd(key, frames);
                            }
                            foreach (var nalu in nalus)
                            {
                                if (nalu.Slice)
                                {
                                    //H264 NALU slice first_mb_in_slice
                                    frames.Add(nalu);
                                }
                                else
                                {
                                    if (nalus.Count > 0)
                                    {
                                        var otherBuffer = FM4Encoder.EncoderOtherVideoBox(frames);
                                        foreach (var session in otherHttpSessions)
                                        {
                                            if (otherBuffer != null)
                                            {
                                                HttpSessionManager.SendAVData(session, otherBuffer, false);
                                            }
                                        }
                                        frames.Clear();
                                    }
                                    frames.Add(nalu);
                                }
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
