using JT1078.Gateway.Abstractions;
using JT1078.Gateway.Sessions;
using JT1078.Hls;
using JT1078.Protocol;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace JT1078.Gateway.TestNormalHosting.Services
{
    public class JT1078HlsNormalMsgHostedService : BackgroundService
    {
        private IJT1078MsgConsumer MsgConsumer;
        private JT1078HttpSessionManager HttpSessionManager;
        private  M3U8FileManage M3U8FileManage;
        public JT1078HlsNormalMsgHostedService(
            M3U8FileManage M3U8FileManage,
            JT1078HttpSessionManager httpSessionManager,
            IJT1078MsgConsumer msgConsumer)
        {
            MsgConsumer = msgConsumer;
            HttpSessionManager = httpSessionManager;
            this.M3U8FileManage = M3U8FileManage;
        }
        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            MsgConsumer.OnMessage((Message) =>
            {
                JT1078Package package = JT1078Serializer.Deserialize(Message.Data);
                var merge = JT1078.Protocol.JT1078Serializer.Merge(package);
                if (merge != null)
                {
                    var hasHttpSessionn = HttpSessionManager.GetAllHttpContextBySimAndChannelNo(merge.SIM, merge.LogicChannelNumber);
                    if (hasHttpSessionn.Count>0)
                    {
                        M3U8FileManage.CreateTsData(merge);
                    }
                    else {
                        M3U8FileManage.Clear(merge.SIM, merge.LogicChannelNumber);
                    }
                }
            });
            return Task.CompletedTask;
        }
    }
}