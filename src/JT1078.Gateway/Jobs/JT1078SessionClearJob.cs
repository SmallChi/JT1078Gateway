using JT1078.Gateway.Abstractions;
using JT1078.Gateway.Configurations;
using JT1078.Gateway.Services;
using JT1078.Gateway.Sessions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;


namespace JT1078.Gateway.Jobs
{
    /// <summary>
    /// 清理hls session
    /// </summary>
    public class JT1078SessionClearJob : BackgroundService
    {
        private readonly ILogger logger;
        private readonly JT1078HttpSessionManager HttpSessionManager;//用户链接session
        private readonly JT1078SessionManager SessionManager;//设备链接session
        private readonly HLSPathStorage hLSPathStorage;
        private readonly JT1078Configuration Configuration;
        public JT1078SessionClearJob(
            ILoggerFactory loggerFactory,
            JT1078SessionManager SessionManager,
            HLSPathStorage hLSPathStorage,
             IOptions<JT1078Configuration> jT1078ConfigurationAccessor,
            [AllowNull]JT1078HttpSessionManager jT1078HttpSessionManager=null)
        {
            logger = loggerFactory.CreateLogger<JT1078SessionClearJob>();
            HttpSessionManager = jT1078HttpSessionManager;
            this.hLSPathStorage = hLSPathStorage;
            this.SessionManager = SessionManager;
            this.Configuration = jT1078ConfigurationAccessor.Value;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await Task.Run(() => {
                while (true)
                {
                    try
                    {
                        var hasSessions = HttpSessionManager.GetAll().Where(m => DateTime.Now.Subtract(m.StartTime).TotalSeconds > 60 && m.RTPVideoType == Metadata.RTPVideoType.Http_Hls).ToList();//所有http 的 hls短链接
                       foreach (var item in hasSessions)
                       {
                            var key = $"{item.Sim}_{item.ChannelNo}";
                            HttpSessionManager.TryRemove(item.SessionId);//超过120s未访问。
                            //清楚所有hls文件
                            string filepath = Path.Combine(Configuration.HlsRootDirectory, key);
                            if (Directory.Exists(filepath)) 
                            {
                                Directory.Delete(filepath,true);
                            }
                            hLSPathStorage.RemoveAllPath(key);//移除所有缓存
                            if (logger.IsEnabled(LogLevel.Debug)) 
                            {
                                logger.LogDebug($"{System.Text.Json.JsonSerializer.Serialize(item)},清除session");
                            }
                            string sim = item.Sim.TrimStart('0');
                            var hasTcpSession = HttpSessionManager.GetAllBySimAndChannelNo(sim, item.ChannelNo).Any(m => m.IsWebSocket);//是否存在tcp的 socket链接
                            var httpFlvSession = HttpSessionManager.GetAllBySimAndChannelNo(sim, item.ChannelNo).Any(m => m.RTPVideoType == Metadata.RTPVideoType.Http_Flv);//是否存在http的 flv长链接
                            if (!hasTcpSession && !httpFlvSession)
                            {
                                //不存在websocket链接和http-flv链接时，主动断开设备链接以节省流量
                                //移除tcpsession,断开设备链接
                                if(SessionManager!=null) SessionManager.RemoveByTerminalPhoneNo(sim);
                            }
                        }
                    }
                    catch(Exception ex)
                    {
                        logger.LogError(ex, ex.Message);
                    }
                    Thread.Sleep(TimeSpan.FromSeconds(30));//30s 执行一次
                }            
            }, stoppingToken);
        }
    }
}
