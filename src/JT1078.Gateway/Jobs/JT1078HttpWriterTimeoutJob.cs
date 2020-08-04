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
                        //过滤掉websocket的方式，无论是客户端主动断开还是关闭浏览器都会有断开通知的情况，所以就只需要判断http的写超时
                        if (!item.IsWebSocket)  
                        {
                            if (item.ActiveTime.AddSeconds(Configuration.CurrentValue.HttpWriterIdleTimeSeconds) < DateTime.Now)
                            {
                                SessionManager.TryRemove(item.SessionId);
                            }
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
