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

namespace JT1078.Gateway.TestNormalHosting.Services
{
    public class JT1078FlvNormalMsgHostedService : BackgroundService
    {
        private IJT1078PackageConsumer PackageConsumer;
        private JT1078HttpSessionManager HttpSessionManager;
        private FlvEncoder FlvEncoder;
        public JT1078FlvNormalMsgHostedService(
            FlvEncoder flvEncoder,
            JT1078HttpSessionManager httpSessionManager,
            IJT1078PackageConsumer packageConsumer)
        {
            PackageConsumer = packageConsumer;
            HttpSessionManager = httpSessionManager;
            FlvEncoder = flvEncoder;
        }
        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            PackageConsumer.OnMessage((Message) =>
            {
                var merge = JT1078.Protocol.JT1078Serializer.Merge(Message.Data);
                if (merge != null)
                {
                    var httpSessions = HttpSessionManager.GetAllBySimAndChannelNo(Message.Data.SIM, Message.Data.LogicChannelNumber);
                    var firstHttpSessions = httpSessions.Where(w => !w.FirstSend).ToList();
                    if (firstHttpSessions.Count > 0)
                    {
                        var flvVideoBuffer = FlvEncoder.EncoderVideoTag(merge, true);
                        HttpSessionManager.SendAVData(firstHttpSessions, flvVideoBuffer, true);
                    }
                    var otherHttpSessions = httpSessions.Where(w => w.FirstSend).ToList();
                    if (otherHttpSessions.Count > 0)
                    {
                        var flvVideoBuffer = FlvEncoder.EncoderVideoTag(merge, false);
                        HttpSessionManager.SendAVData(otherHttpSessions, flvVideoBuffer, false);
                    }
                }
            });
            return Task.CompletedTask;
        }
    }
}
