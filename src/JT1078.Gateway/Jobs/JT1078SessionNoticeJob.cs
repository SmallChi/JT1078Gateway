using JT1078.Gateway.Services;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
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
        public JT1078SessionNoticeJob(
            JT1078SessionNoticeService sessionNoticeService,
            ILoggerFactory loggerFactory)
        {
            logger = loggerFactory.CreateLogger<JT1078SessionNoticeJob>();
            SessionNoticeService = sessionNoticeService;
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
                            logger.LogInformation($"[Notice]:{notice.TerminalPhoneNo}-{notice.ProtocolType}-{notice.SessionType}");
                        }
                    }
                }
                catch
                {

                }
            }, stoppingToken);
        }
    }
}
