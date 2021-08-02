using JT1078.Gateway.Abstractions;
using JT1078.Protocol;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace JT1078.Gateway.TestNormalHosting.Services
{
    /// <summary>
    /// 消费分发服务。同时分发给hls和flv
    /// </summary>
    public class MessageDispatchHostedService : BackgroundService
    {
        private IJT1078MsgConsumer JT1078MsgConsumer;
        private readonly MessageDispatchDataService messageDispatchDataService;

        public MessageDispatchHostedService(IJT1078MsgConsumer JT1078MsgConsumer,
                                            MessageDispatchDataService messageDispatchDataService) {
            this.JT1078MsgConsumer = JT1078MsgConsumer;
            this.messageDispatchDataService = messageDispatchDataService;
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            JT1078MsgConsumer.OnMessage(async (Message) =>
            {
                JT1078Package package = JT1078Serializer.Deserialize(Message.Data);
                var merge = JT1078.Protocol.JT1078Serializer.Merge(package);
                if (merge != null)
                {
                    await messageDispatchDataService.HlsChannel.Writer.WriteAsync(merge, stoppingToken);
                    await messageDispatchDataService.FlvChannel.Writer.WriteAsync(merge, stoppingToken);
                    await messageDispatchDataService.FMp4Channel.Writer.WriteAsync(merge, stoppingToken);
                }
            });
            return Task.CompletedTask;
        }
    }
}
