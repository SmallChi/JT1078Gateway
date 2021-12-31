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
    public class JT1078FMp4NormalMsgHostedService : IHostedService
    {
        private JT1078HttpSessionManager HttpSessionManager;
        private FMp4Encoder FM4Encoder;
        private ILogger Logger;
        private const string ikey = "IFMp4KEY";
        private MessageDispatchDataService messageDispatchDataService;
        private ConcurrentDictionary<string, FMp4AVContext> avFrameDict;
        private H264Decoder H264Decoder;
        List<NalUnitType> NaluFilter;
        BlockingCollection<(string SIM, byte ChannelNo,byte[]FirstBuffer, byte[] OtherBuffer)> FMp4Blocking;
        public JT1078FMp4NormalMsgHostedService(
            MessageDispatchDataService messageDispatchDataService,
            ILoggerFactory loggerFactory,
            FMp4Encoder fM4Encoder,
            H264Decoder h264Decoder,
            JT1078HttpSessionManager httpSessionManager)
        {
            Logger = loggerFactory.CreateLogger<JT1078FMp4NormalMsgHostedService>();
            HttpSessionManager = httpSessionManager;
            FM4Encoder = fM4Encoder;
            H264Decoder= h264Decoder;
            this.messageDispatchDataService = messageDispatchDataService;
            avFrameDict = new ConcurrentDictionary<string, FMp4AVContext>();
            FMp4Blocking=new BlockingCollection<(string SIM, byte ChannelNo, byte[] FirstBuffer, byte[] OtherBuffer)>();
            NaluFilter = new List<NalUnitType>();
            NaluFilter.Add(NalUnitType.SEI);
            NaluFilter.Add(NalUnitType.PPS);
            NaluFilter.Add(NalUnitType.SPS);
            NaluFilter.Add(NalUnitType.AUD);
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            Task.Run(async () => {
                while (!cancellationToken.IsCancellationRequested)
                {
                    var data = await messageDispatchDataService.FMp4Channel.Reader.ReadAsync();
                    try
                    {
                        if (Logger.IsEnabled(LogLevel.Debug))
                        {
                            Logger.LogDebug(JsonSerializer.Serialize(HttpSessionManager.GetAll()));
                            Logger.LogDebug($"{data.SIM},{data.SN},{data.LogicChannelNumber},{data.Label3.DataType.ToString()},{data.Label3.SubpackageType.ToString()},{data.Bodies.ToHexString()}");
                        }
                        List<H264NALU> h264NALUs = H264Decoder.ParseNALU(data);
                        if (h264NALUs!=null && h264NALUs.Count>0)
                        {
                            if(!avFrameDict.TryGetValue(data.GetKey(),out FMp4AVContext cacheFrame))
                            {
                                cacheFrame=new FMp4AVContext();
                                avFrameDict.TryAdd(data.GetKey(), cacheFrame);
                            }
                            foreach(var nalu in h264NALUs)
                            {
                                if (NaluFilter.Contains(nalu.NALUHeader.NalUnitType))
                                {
                                    if (nalu.NALUHeader.NalUnitType== NalUnitType.SPS)
                                    {
                                        cacheFrame.SPSNalu=nalu;
                                    }
                                    else if (nalu.NALUHeader.NalUnitType== NalUnitType.PPS)
                                    {
                                        cacheFrame.PPSNalu=nalu;
                                    }
                                }
                                else
                                {
                                    cacheFrame.NALUs.Add(nalu);
                                }
                            }
                            if (cacheFrame.NALUs.Count>1)
                            {
                                if (cacheFrame.FirstCacheBuffer==null)
                                {
                                    cacheFrame.FirstCacheBuffer=FM4Encoder.FirstVideoBox(cacheFrame.SPSNalu, cacheFrame.PPSNalu);
                                }
                                List<H264NALU> tmp = new List<H264NALU>();
                                int i = 0;
                                foreach (var item in cacheFrame.NALUs)
                                {
                                    if (item.NALUHeader.KeyFrame)
                                    {
                                        if (tmp.Count>0)
                                        {
                                            FMp4Blocking.Add((data.SIM, data.LogicChannelNumber, cacheFrame.FirstCacheBuffer, FM4Encoder.OtherVideoBox(tmp)));
                                            i+=tmp.Count;
                                            tmp.Clear();
                                        }
                                        tmp.Add(item);
                                        i+=tmp.Count;
                                        FMp4Blocking.Add((data.SIM, data.LogicChannelNumber, cacheFrame.FirstCacheBuffer, FM4Encoder.OtherVideoBox(tmp)));
                                        tmp.Clear();
                                        cacheFrame.PrevPrimaryNalu = item;
                                        continue;
                                    }
                                    if (cacheFrame.PrevPrimaryNalu!=null) //第一帧I帧
                                    {
                                        if (tmp.Count>1)
                                        {
                                            FMp4Blocking.Add((data.SIM, data.LogicChannelNumber, cacheFrame.FirstCacheBuffer, FM4Encoder.OtherVideoBox(tmp)));
                                            i+=tmp.Count;
                                            tmp.Clear();
                                        }
                                        tmp.Add(item);
                                    }
                                }
                                cacheFrame.NALUs.RemoveRange(0, i);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.LogError(ex, $"{data.SIM},{data.SN},{data.LogicChannelNumber},{data.Label3.DataType.ToString()},{data.Label3.SubpackageType.ToString()},{data.Bodies.ToHexString()}");
                    }

                }
            });
            Task.Run(() => {
                try
                {
                    foreach(var item in FMp4Blocking.GetConsumingEnumerable(cancellationToken))
                    {
                        var httpSessions = HttpSessionManager.GetAllBySimAndChannelNo(item.SIM.TrimStart('0'), item.ChannelNo);
                        var firstHttpSessions = httpSessions.Where(w => !w.FirstSend && (w.RTPVideoType == Metadata.RTPVideoType.Http_FMp4 || w.RTPVideoType == Metadata.RTPVideoType.Ws_FMp4)).ToList();
                        var otherHttpSessions = httpSessions.Where(w => w.FirstSend && (w.RTPVideoType == Metadata.RTPVideoType.Http_FMp4 || w.RTPVideoType == Metadata.RTPVideoType.Ws_FMp4)).ToList();
                        if (firstHttpSessions.Count > 0)
                        {
                            //首帧
                            foreach (var session in firstHttpSessions)
                            {
                                HttpSessionManager.SendAVData(session, item.FirstBuffer, true);
                                HttpSessionManager.SendAVData(session, item.OtherBuffer, false);
                            }
                        }
                        if (otherHttpSessions.Count > 0)
                        {
                            //非首帧
                            foreach (var session in otherHttpSessions)
                            {
                                HttpSessionManager.SendAVData(session, item.OtherBuffer, false);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {

                }
            });
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public class FMp4AVContext
        {
            public byte[] FirstCacheBuffer { get; set; }
            public H264NALU PrevPrimaryNalu { get; set; }
            public H264NALU SPSNalu { get; set; }
            public H264NALU PPSNalu { get; set; }
            public List<H264NALU> NALUs { get; set; } = new List<H264NALU>();
        }
    }
}
