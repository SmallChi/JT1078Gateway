using JT1078.Gateway.Abstractions;
using JT1078.Gateway.Sessions;
using JT1078.Hls;
using JT1078.Protocol;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
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
        private MessageDispatchDataService messageDispatchDataService;
        private readonly ILogger logger;
        public JT1078HlsNormalMsgHostedService(
            ILoggerFactory loggerFactory,
            M3U8FileManage M3U8FileManage,
            JT1078HttpSessionManager httpSessionManager,
            MessageDispatchDataService messageDispatchDataService,
            IJT1078MsgConsumer msgConsumer)
        {
            logger = loggerFactory.CreateLogger<JT1078HlsNormalMsgHostedService>();
            MsgConsumer = msgConsumer;
            HttpSessionManager = httpSessionManager;
            this.M3U8FileManage = M3U8FileManage;
            this.messageDispatchDataService = messageDispatchDataService;
        }
        protected async override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                var data = await messageDispatchDataService.HlsChannel.Reader.ReadAsync();
                logger.LogDebug($"设备{data.SIM},{data.LogicChannelNumber},session:{System.Text.Json.JsonSerializer.Serialize(HttpSessionManager)}");
                var hasHttpSessionn = HttpSessionManager.GetAllHttpContextBySimAndChannelNo(data.SIM, data.LogicChannelNumber).Where(m => m.RTPVideoType == Metadata.RTPVideoType.Http_Hls).ToList();
                if (hasHttpSessionn.Count > 0)
                {
                    logger.LogDebug($"设备{data.SIM},{data.LogicChannelNumber}连上了");
                    M3U8FileManage.CreateTsData(data);
                }
                else
                {
                    logger.LogDebug($"没有设备链接");
                }
            }
            await Task.CompletedTask;
        }
    }
}