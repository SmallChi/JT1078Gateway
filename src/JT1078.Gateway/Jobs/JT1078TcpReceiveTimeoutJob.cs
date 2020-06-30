using JT1078.Gateway.Configurations;
using JT1078.Gateway.Sessions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace JT1078.Gateway.Jobs
{
    internal class JT1078TcpReceiveTimeoutJob : BackgroundService
    {
        private readonly ILogger Logger;

        private readonly JT1078SessionManager SessionManager;

        private readonly IOptionsMonitor<JT1078Configuration> Configuration;
        public JT1078TcpReceiveTimeoutJob(
                IOptionsMonitor<JT1078Configuration> jT1078ConfigurationAccessor,
                ILoggerFactory loggerFactory,
                JT1078SessionManager jT1078SessionManager
            )
        {
            SessionManager = jT1078SessionManager;
            Logger = loggerFactory.CreateLogger<JT1078TcpReceiveTimeoutJob>();
            Configuration = jT1078ConfigurationAccessor;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    foreach (var item in SessionManager.GetTcpAll())
                    {
                        if (item.ActiveTime.AddSeconds(Configuration.CurrentValue.TcpReaderIdleTimeSeconds) < DateTime.Now)
                        {
                            item.ReceiveTimeout.Cancel();
                        }
                    }
                    if (Logger.IsEnabled(LogLevel.Information))
                    {
                        Logger.LogInformation($"[Check Receive Timeout]");
                        Logger.LogInformation($"[Session Online Count]:{SessionManager.TcpSessionCount}");
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, $"[Receive Timeout]");
                }
                finally
                {
                    await Task.Delay(TimeSpan.FromSeconds(Configuration.CurrentValue.TcpReceiveTimeoutCheckTimeSeconds), stoppingToken);
                }
            }
        }
    }
}
