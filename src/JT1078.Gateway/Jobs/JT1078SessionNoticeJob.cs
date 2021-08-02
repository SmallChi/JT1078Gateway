using JT1078.Gateway.Abstractions;
using JT1078.Gateway.Services;
using JT1078.Gateway.Sessions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;


namespace JT1078.Gateway.Jobs
{
    public class JT1078SessionNoticeJob : BackgroundService
    {
        private readonly ILogger logger;
        private JT1078SessionNoticeService SessionNoticeService;
        private JT1078HttpSessionManager HttpSessionManager;
        public JT1078SessionNoticeJob(
            JT1078SessionNoticeService sessionNoticeService,
            ILoggerFactory loggerFactory,
            [AllowNull]JT1078HttpSessionManager jT1078HttpSessionManager=null)
        {
            logger = loggerFactory.CreateLogger<JT1078SessionNoticeJob>();
            SessionNoticeService = sessionNoticeService;
            HttpSessionManager = jT1078HttpSessionManager;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await Task.Run(() => {
                try
                {
                    foreach (var notice in SessionNoticeService.SessionNoticeBlockingCollection.GetConsumingEnumerable(stoppingToken))
                    {
                        if (logger.IsEnabled(LogLevel.Information))
                        {
                            logger.LogInformation($"[Notice]:{notice.SIM}-{notice.ProtocolType}-{notice.SessionType}");
                        }
                        if(JT1078GatewayConstants.SessionOffline== notice.SessionType)
                        {
                            if (HttpSessionManager != null)
                            {
                                //当1078设备主动断开的情况下，需要关闭所有再观看的连接
                                HttpSessionManager.TryRemoveBySim(notice.SIM);
                            }
                        }
                    }
                }
                catch(Exception ex)
                {
                    logger.LogError(ex, "");
                }
            }, stoppingToken);
        }
    }
}
