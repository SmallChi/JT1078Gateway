using JT1078.Gateway.Configurations;
using JT1078.Gateway.Extensions;
using JT1078.Gateway.Metadata;
using JT1078.Gateway.Sessions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
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
        private const string m3u8Mime = "application/x-mpegURL";
        private const string tsMime = "video/MP2T";
        private readonly JT1078Configuration Configuration;
        private readonly JT1078HttpSessionManager HttpSessionManager;
        private readonly JT1078SessionManager SessionManager;
        private readonly ILogger Logger;
        private IMemoryCache memoryCache;
        private FileSystemWatcher fileSystemWatcher;

        public HLSRequestManager(
                                                    IMemoryCache memoryCache,
                                                    IOptions<JT1078Configuration> jT1078ConfigurationAccessor,
                                                    JT1078HttpSessionManager httpSessionManager,
                                                    JT1078SessionManager sessionManager,
                                                    FileSystemWatcher fileSystemWatcher,
                                                     ILoggerFactory loggerFactory)
        {
            this.memoryCache = memoryCache;
            this.fileSystemWatcher = fileSystemWatcher;
            HttpSessionManager = httpSessionManager;
            SessionManager = sessionManager;
            Configuration = jT1078ConfigurationAccessor.Value;
            Logger = loggerFactory.CreateLogger<HLSRequestManager>();

            Task.Run(()=> {
                while (true)
                {
                    var expireds= HttpSessionManager.GetAll().Where(m => DateTime.Now.Subtract(m.StartTime).TotalSeconds > 20).ToList();
                    foreach (var item in expireds)
                    {
                        //移除httpsession
                        HttpSessionManager.TryRemoveBySim(item.Sim);
                        //移除tcpsession
                        SessionManager.RemoveByTerminalPhoneNo(item.Sim);
                    }
                    Thread.Sleep(TimeSpan.FromSeconds(10));
                }            
            });
        }
        /// <summary>
        /// 处理hls实时视频请求
        /// </summary>
        /// <param name="context"></param>
        /// <param name="principal"></param>
        public async void HandleHlsRequest(HttpListenerContext context, IPrincipal principal) {
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
            if (!File.Exists(filepath))
            {
                if (filename.ToLower().Contains("m3u8"))
                {
                    fileSystemWatcher = new FileSystemWatcher();
                    fileSystemWatcher.Path = Path.Combine(Configuration.HlsRootDirectory, key);
                    fileSystemWatcher.NotifyFilter = NotifyFilters.LastWrite;  //NotifyFilters.CreateTime
                    fileSystemWatcher.Filter = "*.m3u8";                    // Only watch text files.
                    fileSystemWatcher.Changed += (sender, arg) =>
                    {
                        if (context.Response.ContentLength64 != 0) return;
                        //wwwroot\1234_2\live.m3u8
                        //var key = arg.FullPath.Replace(arg.Name, "").Substring(arg.FullPath.Replace(arg.Name, "").IndexOf("\\")).Replace("\\", "");
                        var key = arg.FullPath.Substring(arg.FullPath.IndexOf("\\")+1,arg.FullPath.LastIndexOf("\\"));
                        var sim = key.Split("_")[0];
                        var channel = int.Parse(key.Split("_")[1]);
                        try
                        {
                            using (FileStream sr = new FileStream(arg.FullPath, FileMode.Open))
                            {
                                context.Response.ContentType = m3u8Mime;
                                context.Response.StatusCode = (int)HttpStatusCode.OK;
                                context.Response.ContentLength64 = sr.Length;
                                sr.CopyTo(context.Response.OutputStream);
                            }
                        }
                        catch (Exception ex)
                        {
                            Logger.LogError(ex, $"{context.Request.Url}");
                        }
                        finally
                        {
                            context.Response.OutputStream.Close();
                            context.Response.Close();
                        }
                    };
                    fileSystemWatcher.EnableRaisingEvents = true;         // Begin watching.
                }
                else
                {
                    context.Http404();
                    return;
                }
            }
            else
            {
                try
                {
                    using (FileStream sr = new FileStream(filepath, FileMode.Open))
                    {
                        if (filename.ToLower().Contains("m3u8")) 
                        {
                            context.Response.ContentType = m3u8Mime;
                        }
                        else
                        {
                            context.Response.ContentType = tsMime;
                        }
                        context.Response.StatusCode = (int)HttpStatusCode.OK;
                        context.Response.ContentLength64 = sr.Length;
                        await sr.CopyToAsync(context.Response.OutputStream);
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, $"{context.Request.Url}");
                    context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                }
                finally
                {
                    context.Response.OutputStream.Close();
                    context.Response.Close();
                }
            }
            var jT1078HttpContext = new JT1078HttpContext(context, principal);
            jT1078HttpContext.Sim = sim;
            jT1078HttpContext.ChannelNo = int.Parse(channelNo);
            jT1078HttpContext.RTPVideoType = RTPVideoType.Http_Hls;
            HttpSessionManager.AddOrUpdate(jT1078HttpContext);
        }
    }
}
