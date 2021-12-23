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
        List<JT1078Package> SegmentPackages = new List<JT1078Package>();// 一段包 以I帧为界 IPPPP ，  IPPPP 一组
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

                    if (data.Label3.DataType == Protocol.Enums.JT1078DataType.视频I帧)
                    {
                        if (SegmentPackages.Count>0)
                        {
                            //判断是否首帧
                            //Logger.LogDebug($"时间1：{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss ffff")}");
                            var httpSessions = HttpSessionManager.GetAllBySimAndChannelNo(data.SIM.TrimStart('0'), data.LogicChannelNumber);
                            var firstHttpSessions = httpSessions.Where(w => !w.FirstSend && (w.RTPVideoType == Metadata.RTPVideoType.Http_FMp4 || w.RTPVideoType == Metadata.RTPVideoType.Ws_FMp4)).ToList();
                            var otherHttpSessions = httpSessions.Where(w => w.FirstSend && (w.RTPVideoType == Metadata.RTPVideoType.Http_FMp4 || w.RTPVideoType == Metadata.RTPVideoType.Ws_FMp4)).ToList();
                            if (firstHttpSessions.Count > 0)
                            {
                                //唯一
                                var ftyp = FM4Encoder.FtypBox();
                                var package1 = SegmentPackages[0];
                                var nalus1 = H264Decoder.ParseNALU(package1);
                                var moov = FM4Encoder.MoovBox(
                                  nalus1.FirstOrDefault(f => f.NALUHeader.NalUnitType == NalUnitType.SPS),
                                  nalus1.FirstOrDefault(f => f.NALUHeader.NalUnitType == NalUnitType.PPS));
                                //首帧
                                var styp = FM4Encoder.StypBox();
                                var firstVideo = FM4Encoder.OtherVideoBox(SegmentPackages);
                                foreach (var session in firstHttpSessions)
                                {
                                    HttpSessionManager.SendAVData(session, ftyp.Concat(moov).Concat(styp).Concat(firstVideo).ToArray(), true);
                                    SegmentPackages.Clear();//发送完成后清理
                                }
                            }
                            if (otherHttpSessions.Count > 0)
                            {
                                //非首帧
                                var styp = FM4Encoder.StypBox();
                                var otherVideo = FM4Encoder.OtherVideoBox(SegmentPackages);
                                foreach (var session in otherHttpSessions)
                                {
                                    HttpSessionManager.SendAVData(session, styp.Concat(otherVideo).ToArray(), false);
                                    SegmentPackages.Clear();//发送完成后清理
                                }

                            }
                        }

                        if (SegmentPackages.Count==0)
                            SegmentPackages.Add(data);
                    }
                    else {
                        if (SegmentPackages.Count!=0) {
                            SegmentPackages.Add(data);
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
