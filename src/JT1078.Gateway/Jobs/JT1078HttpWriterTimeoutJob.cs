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
    internal class JT1078HttpWriterTimeoutJob : BackgroundService
    {
        private readonly ILogger Logger;

        private readonly JT1078HttpSessionManager SessionManager;

        private readonly IOptionsMonitor<JT1078Configuration> Configuration;
        public JT1078HttpWriterTimeoutJob(
                IOptionsMonitor<JT1078Configuration> jT1078ConfigurationAccessor,
                ILoggerFactory loggerFactory,
                JT1078HttpSessionManager jT1078SessionManager
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
                    foreach (var item in SessionManager.GetAll())
                    {
                        if (item.ActiveTime.AddSeconds(Configuration.CurrentValue.HttpWriterIdleTimeSeconds) < DateTime.Now)
                        {
                            SessionManager.TryRemove(item.SessionId);
                        }
                    }
                    if (Logger.IsEnabled(LogLevel.Information))
                    {
                        Logger.LogInformation($"[Http Check Writer Timeout]");
                        Logger.LogInformation($"[Http Session Online Count]:{SessionManager.SessionCount}");
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, $"[Http Writer Timeout]");
                }
                finally
                {
                    await Task.Delay(TimeSpan.FromSeconds(Configuration.CurrentValue.HttpWriterTimeoutCheckTimeSeconds), stoppingToken);
                }
            }
        }
    }
}
