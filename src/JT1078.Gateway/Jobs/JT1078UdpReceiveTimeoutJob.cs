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
    internal class JT1078UdpReceiveTimeoutJob : BackgroundService
    {
        private readonly ILogger Logger;

        private readonly JT1078SessionManager SessionManager;

        private readonly IOptionsMonitor<JT1078Configuration> Configuration;
        public JT1078UdpReceiveTimeoutJob(
                IOptionsMonitor<JT1078Configuration> jT1078ConfigurationAccessor,
                ILoggerFactory loggerFactory,
                JT1078SessionManager jT1078SessionManager
            )
        {
            SessionManager = jT1078SessionManager;
            Logger = loggerFactory.CreateLogger<JT1078UdpReceiveTimeoutJob>();
            Configuration = jT1078ConfigurationAccessor;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    List<string> sessionIds = new List<string>();
                    foreach (var item in SessionManager.GetUdpAll())
                    {
                        if (item.ActiveTime.AddSeconds(Configuration.CurrentValue.UdpReaderIdleTimeSeconds) < DateTime.Now)
                        {
                            sessionIds.Add(item.SessionID);
                        }
                    }
                    foreach (var item in sessionIds)
                    {
                        SessionManager.RemoveBySessionId(item);
                    }
                    if (Logger.IsEnabled(LogLevel.Information))
                    {
                        Logger.LogInformation($"[Check Receive Timeout]");
                        Logger.LogInformation($"[Session Online Count]:{SessionManager.UdpSessionCount}");
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, $"[Receive Timeout]");
                }
                finally
                {
                    await Task.Delay(TimeSpan.FromSeconds(Configuration.CurrentValue.UdpReceiveTimeoutCheckTimeSeconds), stoppingToken);
                }
            }
        }
    }
}
