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
        BlockingCollection<(string SIM, byte ChannelNo,byte[]FirstBuffer, byte[] AVFrameInfoBuffer, byte[] OtherBuffer)> FMp4Blocking;
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
            FMp4Blocking=new BlockingCollection<(string SIM, byte ChannelNo, byte[] FirstBuffer, byte[] AVFrameInfoBuffer, byte[] OtherBuffer)>();
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            Task.Run(async () => {
                while (!cancellationToken.IsCancellationRequested)
                {
                    var data = await messageDispatchDataService.FMp4Channel.Reader.ReadAsync();
                    try
                    {
                        //if (Logger.IsEnabled(LogLevel.Debug))
                        //{
                        //    Logger.LogDebug(JsonSerializer.Serialize(HttpSessionManager.GetAll()));
                        //    Logger.LogDebug($"{data.SIM},{data.SN},{data.LogicChannelNumber},{data.Label3.DataType.ToString()},{data.Label3.SubpackageType.ToString()},{data.Bodies.ToHexString()}");
                        //}
                        bool keyframe = data.Label3.DataType == Protocol.Enums.JT1078DataType.视频I帧;
                        JT1078AVFrame avframe = H264Decoder.ParseAVFrame(data);
                        if (avframe.Nalus!= null && avframe.Nalus.Count>0)
                        {
                            if(!avFrameDict.TryGetValue(data.GetAVKey(),out FMp4AVContext cacheFrame))
                            {
                                cacheFrame=new FMp4AVContext();
                                avFrameDict.TryAdd(data.GetAVKey(), cacheFrame);
                            }
                            if(keyframe)
                            {
                                if(avframe.SPS!=null && avframe.PPS != null)
                                {
                                    cacheFrame.AVFrameInfoBuffer = JsonSerializer.SerializeToUtf8Bytes(
                                        new { Codecs = avframe .ToCodecs(), Width = avframe .Width, Height =avframe.Height});
                                    cacheFrame.FirstCacheBuffer = FM4Encoder.FirstVideoBox(avframe);
                                }
                            }
                            if (cacheFrame.FirstCacheBuffer != null)
                            {
                                FMp4Blocking.Add((data.SIM, data.LogicChannelNumber, cacheFrame.FirstCacheBuffer, cacheFrame.AVFrameInfoBuffer,FM4Encoder.OtherVideoBox(avframe.Nalus, data.GetAVKey(), keyframe)));
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
                //var filepath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "JT1078_7_4_4.mp4");
                //if (File.Exists(filepath))
                //{
                //    File.Delete(filepath);
                //}
                //using var fileStream = new FileStream(filepath, FileMode.OpenOrCreate, FileAccess.Write);
                try
                {
                    foreach (var item in FMp4Blocking.GetConsumingEnumerable(cancellationToken))
                    {
                        var httpSessions = HttpSessionManager.GetAllBySimAndChannelNo(item.SIM.TrimStart('0'), item.ChannelNo);
                        var firstHttpSessions = httpSessions.Where(w => !w.FirstSend && (w.RTPVideoType == Metadata.RTPVideoType.Http_FMp4 || w.RTPVideoType == Metadata.RTPVideoType.Ws_FMp4)).ToList();
                        var otherHttpSessions = httpSessions.Where(w => w.FirstSend && (w.RTPVideoType == Metadata.RTPVideoType.Http_FMp4 || w.RTPVideoType == Metadata.RTPVideoType.Ws_FMp4)).ToList();
                        if (firstHttpSessions.Count > 0)
                        {
                            //首帧
                            foreach (var session in firstHttpSessions)
                            {
                                HttpSessionManager.SendAVData(session, item.AVFrameInfoBuffer, true);
                                HttpSessionManager.SendAVData(session, item.FirstBuffer, false);
                                //fileStream.Write(item.FirstBuffer);
                                HttpSessionManager.SendAVData(session, item.OtherBuffer, false);
                                //fileStream.Write(item.OtherBuffer);
                            }
                        }
                        if (otherHttpSessions.Count > 0)
                        {
                            //非首帧
                            foreach (var session in otherHttpSessions)
                            {
                                HttpSessionManager.SendAVData(session, item.OtherBuffer, false);
                                //fileStream.Write(item.OtherBuffer);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {

                }
                //fileStream.Close();
            });
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public class FMp4AVContext
        {
            public byte[] AVFrameInfoBuffer { get; set; }
            public byte[] FirstCacheBuffer { get; set; }
        }
    }
}
