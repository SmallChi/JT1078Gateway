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
using System.Text.Json;
using System.IO;

namespace JT1078.Gateway.Jobs
{
    public class JT1078HeartbeatJob : BackgroundService
    {
        private readonly ILogger Logger;

        private readonly JT1078SessionManager SessionManager;

        private readonly JT1078HttpSessionManager HttpSessionManager;

        private readonly IOptionsMonitor<JT1078Configuration> Configuration;

        private readonly JT1078CoordinatorHttpClient CoordinatorHttpClient;
        public JT1078HeartbeatJob(
                JT1078CoordinatorHttpClient jT1078CoordinatorHttpClient,
                JT1078HttpSessionManager jT1078HttpSessionManager,
                IOptionsMonitor<JT1078Configuration> jT1078ConfigurationAccessor,
                ILoggerFactory loggerFactory,
                JT1078SessionManager jT1078SessionManager
            )
        {
            SessionManager = jT1078SessionManager;
            HttpSessionManager = jT1078HttpSessionManager;
            Logger = loggerFactory.CreateLogger<JT1078HeartbeatJob>();
            Configuration = jT1078ConfigurationAccessor;
            CoordinatorHttpClient = jT1078CoordinatorHttpClient;
        }
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                await CoordinatorHttpClient.Reset();
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, $"[Coordinator Reset]");
            }
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    string json = "";
                    using (var stream = new MemoryStream())
                    {
                        using (var writer = new Utf8JsonWriter(stream))
                        {
                            writer.WriteStartObject();
                            writer.WriteNumber(nameof(Configuration.CurrentValue.HttpPort), Configuration.CurrentValue.HttpPort);
                            writer.WriteNumber(nameof(Configuration.CurrentValue.TcpPort), Configuration.CurrentValue.TcpPort);
                            writer.WriteNumber(nameof(Configuration.CurrentValue.UdpPort), Configuration.CurrentValue.UdpPort);
                            writer.WriteNumber(nameof(SessionManager.TcpSessionCount), SessionManager.TcpSessionCount);
                            writer.WriteNumber(nameof(SessionManager.UdpSessionCount), SessionManager.UdpSessionCount);
                            writer.WriteNumber(nameof(HttpSessionManager.HttpSessionCount), HttpSessionManager.HttpSessionCount);
                            writer.WriteNumber(nameof(HttpSessionManager.WebSocketSessionCount), HttpSessionManager.WebSocketSessionCount);
                            writer.WriteStartArray("Sims");
                            var sessions = HttpSessionManager.GetAll();
                            if (sessions != null)
                            {
                                foreach(var session in sessions)
                                {
                                    writer.WriteStringValue($"{session.Sim}_{session.ChannelNo}_{session.SessionId}");
                                }
                            }
                            writer.WriteEndArray();
                            writer.WriteEndObject();
                        }
                        json = Encoding.UTF8.GetString(stream.ToArray());
                    }
                    if (json != "") 
                    {
                        if (Logger.IsEnabled(LogLevel.Information))
                        {
                            Logger.LogInformation(json);
                        }
                        await CoordinatorHttpClient.Heartbeat(json);
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, $"[Coordinator Heartbeat]");
                }
                finally
                {
                    await Task.Delay(TimeSpan.FromSeconds(Configuration.CurrentValue.CoordinatorHeartbeatTimeSeconds), stoppingToken);
                }
            }
        }
    }
}
