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

namespace JT1078.Gateway.Services
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

        public HLSRequestManager(IOptions<JT1078Configuration> jT1078ConfigurationAccessor,
                                JT1078HttpSessionManager httpSessionManager,
                                HLSPathStorage hLSPathStorage,
                                ILoggerFactory loggerFactory)
        {
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
        /// <param name="jT1078AVInfo"></param>
        public async void HandleHlsRequest(HttpListenerContext context, IPrincipal principal, JT1078AVInfo jT1078AVInfo)
        {
            string filename = Path.GetFileName(context.Request.Url.AbsolutePath.ToString());
            string filepath = Path.Combine(Configuration.HlsRootDirectory, jT1078AVInfo.ToString(), filename);
            if (hLSPathStorage.ExsitPath(filepath))
            {
                try
                {
                    using (FileStream sr = new FileStream(filepath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                    {
                        if (filename.Contains("m3u8"))
                        {
                            await context.HttpM3U8Async(sr);
                        }
                        else if (filename.Contains("ts"))
                        {
                            await context.HttpTsAsync(sr);
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
                        var directory = Path.Combine(Configuration.HlsRootDirectory, jT1078AVInfo.ToString());
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
                                        await context.HttpM3U8Async(sr);
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
                    hLSPathStorage.AddPath(filepath, jT1078AVInfo.ToString());
                    using (FileStream sr = new FileStream(filepath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                    {
                        if (filename.Contains("m3u8"))
                        {
                            await context.HttpM3U8Async(sr);
                        }
                        else if (filename.Contains("ts"))
                        {
                            await context.HttpTsAsync(sr);
                        }
                        else
                        {
                            context.Http404();
                        }
                    }
                }
                var jT1078HttpContext = new JT1078HttpContext(context, principal);
                jT1078HttpContext.Sim = jT1078AVInfo.Sim;
                jT1078HttpContext.ChannelNo = jT1078AVInfo.ChannelNo;
                jT1078HttpContext.RTPVideoType = RTPVideoType.Http_Hls;
                HttpSessionManager.AddOrUpdateHlsSession(jT1078HttpContext);
            }
        }
    }
}
