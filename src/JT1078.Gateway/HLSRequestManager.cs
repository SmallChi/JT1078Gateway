using JT1078.Gateway.Configurations;
using JT1078.Gateway.Extensions;
using JT1078.Gateway.Metadata;
using JT1078.Gateway.Sessions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Principal;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace JT1078.Gateway
{
    /// <summary>
    /// Hls请求管理
    /// </summary>
    public class HLSRequestManager
    {
        private readonly JT1078Configuration Configuration;
        private readonly JT1078HttpSessionManager HttpSessionManager;
        private readonly HLSPathStorage hLSPathStorage;
        private readonly ILogger Logger;

        private readonly IServiceProvider serviceProvider;
        //private FileSystemWatcher fileSystemWatcher;

        public HLSRequestManager(
                                                    IOptions<JT1078Configuration> jT1078ConfigurationAccessor,
                                                    JT1078HttpSessionManager httpSessionManager,
                                                    HLSPathStorage hLSPathStorage,
                                                    IServiceProvider serviceProvider,
                                                     ILoggerFactory loggerFactory)
        {
            this.serviceProvider = serviceProvider;
            HttpSessionManager = httpSessionManager;
            this.hLSPathStorage = hLSPathStorage;
            Configuration = jT1078ConfigurationAccessor.Value;
            Logger = loggerFactory.CreateLogger<HLSRequestManager>();
        }
        /// <summary>
        /// 处理hls实时视频请求
        /// </summary>
        /// <param name="context"></param>
        /// <param name="principal"></param>
        public async void HandleHlsRequest(HttpListenerContext context, IPrincipal principal)
        {
            if (context.Request.QueryString.Count < 2)
            {
                context.Http404();
                return;
            }
            string sim = context.Request.QueryString.Get("sim");//终端sim卡号
            string channelNo = context.Request.QueryString.Get("channelNo");//通道号
            string key = $"{sim}_{channelNo}";
            string filename = Path.GetFileName(context.Request.Url.AbsolutePath.ToString());
            string filepath = Path.Combine(Configuration.HlsRootDirectory, key, filename);

            if (hLSPathStorage.ExsitPath(filepath))
            {
                try
                {
                    using (FileStream sr = new FileStream(filepath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                    {
                        MemoryStream ms = new MemoryStream();
                        sr.CopyTo(ms);
                        if (filename.Contains("m3u8"))
                        {
                            await context.HttpM3U8Async(ms.ToArray());
                        }
                        else if (filename.Contains("ts"))
                        {
                            await context.HttpTsAsync(ms.ToArray());
                        }
                        else
                        {
                            context.Http404();
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, ex.Message);
                    context.Http404();
                }        
            }
            else
            {
                if (!File.Exists(filepath))
                {
                    if (filename.ToLower().Contains("m3u8"))
                    {
                        var directory = Path.Combine(Configuration.HlsRootDirectory, key);
                        if (!Directory.Exists(directory))
                        {
                            Directory.CreateDirectory(directory);
                        }
                        if (!hLSPathStorage.ExistFileSystemWatcher(directory)) {
                            var fileSystemWatcher = new FileSystemWatcher();
                            fileSystemWatcher.Path = directory;
                            fileSystemWatcher.NotifyFilter = NotifyFilters.LastWrite;  //NotifyFilters.CreateTime
                            fileSystemWatcher.Filter = "*.m3u8";                    // Only watch text files.
                            fileSystemWatcher.Changed += async (sender, arg) =>
                            {
                                if (context.Response.ContentLength64 != 0) return;
                                //wwwroot\1234_2\live.m3u8
                                //var key = arg.FullPath.Replace(arg.Name, "").Substring(arg.FullPath.Replace(arg.Name, "").IndexOf("\\")).Replace("\\", "");
                                var key = arg.FullPath.Substring(arg.FullPath.IndexOf("\\") + 1, (arg.FullPath.LastIndexOf("\\") - arg.FullPath.IndexOf("\\")) - 1);
                                var sim = key.Split("_")[0];
                                var channel = int.Parse(key.Split("_")[1]);
                                try
                                {
                                    using (FileStream sr = new FileStream(arg.FullPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                                    {
                                        hLSPathStorage.AddPath(arg.FullPath, key);
                                        MemoryStream ms = new MemoryStream();
                                        sr.CopyTo(ms);
                                        await context.HttpM3U8Async(ms.ToArray());
                                    }
                                }
                                catch (Exception ex)
                                {
                                    Logger.LogError(ex, $"{context.Request.Url}");
                                    context.Http404();
                                }
                                finally 
                                {
                                    hLSPathStorage.DeleteFileSystemWatcher(directory);
                                }
                            };
                            fileSystemWatcher.EnableRaisingEvents = true;         // Begin watching.
                            hLSPathStorage.AddFileSystemWatcher(directory, fileSystemWatcher);
                        }
                    }
                    else
                    {
                        context.Http404();
                        return;
                    }
                }
                else
                {
                    hLSPathStorage.AddPath(filepath, key);
                    using (FileStream sr = new FileStream(filepath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                    {
                        MemoryStream ms = new MemoryStream();
                        sr.CopyTo(ms);
                        if (filename.Contains("m3u8"))
                        {
                            await context.HttpM3U8Async(ms.ToArray());
                        }
                        else if (filename.Contains("ts"))
                        {
                            await context.HttpTsAsync(ms.ToArray());
                        }
                        else
                        {
                            context.Http404();
                        }
                    }
                }
                var jT1078HttpContext = new JT1078HttpContext(context, principal);
                jT1078HttpContext.Sim = sim;
                jT1078HttpContext.ChannelNo = int.Parse(channelNo);
                jT1078HttpContext.RTPVideoType = RTPVideoType.Http_Hls;
                HttpSessionManager.AddOrUpdateHlsSession(jT1078HttpContext);
            }
        }
    }
}
