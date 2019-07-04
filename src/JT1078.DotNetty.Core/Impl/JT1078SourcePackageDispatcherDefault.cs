using JT1078.DotNetty.Core.Configurations;
using JT1078.DotNetty.Core.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;

namespace JT1078.DotNetty.Core.Impl
{
    class JT1078SourcePackageDispatcherDefault : IJT1078SourcePackageDispatcher,IDisposable
    {
        private readonly ILogger<JT1078SourcePackageDispatcherDefault> logger;
        private IOptionsMonitor<JT1078Configuration> optionsMonitor;
        private ConcurrentDictionary<string, TcpClient> channeldic = new ConcurrentDictionary<string, TcpClient>();
        private Queue<string> reconnectionQueue = new Queue<string>();
        public JT1078SourcePackageDispatcherDefault(ILoggerFactory loggerFactory,
                                                    IOptionsMonitor<JT1078Configuration> optionsMonitor)
        {
            logger = loggerFactory.CreateLogger<JT1078SourcePackageDispatcherDefault>();
            this.optionsMonitor = optionsMonitor;
            timer = new System.Timers.Timer(10000);
            timer.Elapsed += new ElapsedEventHandler(timer_Elapsed);
            timer.Start();
            InitialDispatcherClient();
        }

        private System.Timers.Timer timer;

        public Task SendAsync(byte[] data)
        {
            foreach (var item in channeldic)
            {
                try
                {
                    if (item.Value.Connected)
                    {
                         item.Value.Client.Send(data);
                    }
                    else
                    {
                        logger.LogError($"{item}链接已关闭");
                        channeldic.TryRemove(item.Key, out _);
                        reconnectionQueue.Enqueue(item.Key);
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError($"{item}发送数据出现异常：{ex}");
                    reconnectionQueue.Enqueue(item.Key);
                    channeldic.TryRemove(item.Key, out _);
                }
            }
            return Task.CompletedTask;
        }

        public void InitialDispatcherClient()
        {
            Task.Run(async () =>
            {
                optionsMonitor.OnChange(options =>
                {
                    List<string> lastRemoteServers = new List<string>();
                    if (options.RemoteServerOptions.RemoteServers != null)
                    {
                        lastRemoteServers = options.RemoteServerOptions.RemoteServers;
                    }
                    DelRemoteServsers(lastRemoteServers);
                    AddRemoteServsers(lastRemoteServers);
                });
                await InitRemoteServsers();
            });
        }

        void timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            timer.Stop();
            Thread.CurrentThread.IsBackground = true;
            var ip = reconnectionQueue.Dequeue();
            if (!string.IsNullOrEmpty(ip))
            {
                AddRemoteServsers(new List<string>() { ip });
            }
            timer.Start();
        }

        /// <summary>
        /// 初始化远程服务器
        /// </summary>
        /// <param name="bootstrap"></param>
        /// <param name="remoteServers"></param>
        /// <returns></returns>
        private async Task InitRemoteServsers()
        {
            List<string> remoteServers = new List<string>();
            if (optionsMonitor.CurrentValue.RemoteServerOptions.RemoteServers != null)
            {
                remoteServers = optionsMonitor.CurrentValue.RemoteServerOptions.RemoteServers;
            }
            foreach (var item in remoteServers)
            {
                try
                {
                    TcpClient client = new TcpClient();
                    client.Connect(new IPEndPoint(IPAddress.Parse(item.Split(':')[0]), int.Parse(item.Split(':')[1])));
                    if (client.Connected)
                    {
                        channeldic.TryAdd(item, client);
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError($"初始化配置链接远程服务端{item},链接异常：{ex}");
                }
            }
            await Task.CompletedTask;
        }

        /// <summary>
        /// 动态删除远程服务器
        /// </summary>
        /// <param name="lastRemoteServers"></param>
        private void DelRemoteServsers(List<string> lastRemoteServers)
        {
            var delChannels = channeldic.Keys.Except(lastRemoteServers).ToList();
            foreach (var item in delChannels)
            {
                channeldic[item].Close();
                channeldic[item].Dispose();
                channeldic.TryRemove(item, out var client);
            }
        }
        /// <summary>
        /// 动态添加远程服务器
        /// </summary>
        /// <param name="bootstrap"></param>
        /// <param name="lastRemoteServers"></param>
        private void AddRemoteServsers(List<string> lastRemoteServers)
        {
            var addChannels = lastRemoteServers.Except(channeldic.Keys).ToList();
            foreach (var item in addChannels)
            {
                try
                {
                    TcpClient client = new TcpClient();
                    client.Connect(new IPEndPoint(IPAddress.Parse(item.Split(':')[0]), int.Parse(item.Split(':')[1])));
                    if (client.Connected)
                    {
                        channeldic.TryAdd(item, client);
                    }
                    else
                    {
                        reconnectionQueue.Enqueue(item);
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError($"变更配置后链接远程服务端{item},重连异常：{ex}");
                    reconnectionQueue.Enqueue(item);
                }
            }
        }

        public void Dispose()
        {
            timer.Stop();
            if (channeldic != null)
            {
                foreach (var item in channeldic)
                {
                    try
                    {
                        if (item.Value.Connected)
                        {
                            item.Value.Close();
                            item.Value.Dispose();
                        }
                    }
                    catch (Exception)
                    {

                    }
                }
            }
            timer.Dispose();
        }
    }
}
