using JT1078.Gateway.Abstractions;
using JT1078.Gateway.Configurations;
using JT1078.Gateway.Metadata;
using JT1078.Gateway.Sessions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.WebSockets;
using System.Security.Principal;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using JT1078.Gateway.Extensions;
using JT1078.Gateway.Services;

namespace JT1078.Gateway
{
    /// <summary>
    /// http服务器
    /// </summary>
    public class JT1078HttpServer : IHostedService
    {
        private readonly ILogger Logger;

        private readonly JT1078Configuration Configuration;

        private readonly IJT1078Authorization authorization;

        private HttpListener listener;

        private JT1078HttpSessionManager SessionManager;

        private HLSRequestManager hLSRequestManager;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="jT1078ConfigurationAccessor"></param>
        /// <param name="authorization"></param>
        /// <param name="sessionManager"></param>
        /// <param name="hLSRequestManager"></param>
        /// <param name="loggerFactory"></param>
        public JT1078HttpServer(
            IOptions<JT1078Configuration> jT1078ConfigurationAccessor,
            IJT1078Authorization authorization,
            JT1078HttpSessionManager sessionManager,
            HLSRequestManager hLSRequestManager,
            ILoggerFactory loggerFactory)
        {
            Logger = loggerFactory.CreateLogger<JT1078TcpServer>();
            Configuration = jT1078ConfigurationAccessor.Value;
            this.authorization = authorization;
            this.SessionManager = sessionManager;
            this.hLSRequestManager = hLSRequestManager;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public Task StartAsync(CancellationToken cancellationToken)
        {
            if (!HttpListener.IsSupported)
            {
                Logger.LogWarning("Windows XP SP2 or Server 2003 is required to use the HttpListener class.");
                return Task.CompletedTask;
            }
            listener = new HttpListener();
            listener.AuthenticationSchemes = AuthenticationSchemes.Anonymous;
            try
            {
                listener.Prefixes.Add($"http://*:{Configuration.HttpPort}/");
                listener.Start();
            }
            catch (System.Net.HttpListenerException ex)
            {
                Logger.LogWarning(ex, $"{ex.Message}:使用cmd命令[netsh http add urlacl url=http://*:{Configuration.HttpPort}/ user=Everyone]");
                return Task.CompletedTask;
            }
            Logger.LogInformation($"JT1078 Http Server start at {IPAddress.Any}:{Configuration.HttpPort}.");
            Task.Factory.StartNew(async () =>
            {
                while (listener.IsListening)
                {
                    var context = await listener.GetContextAsync();
                    try
                    {
                        await Task.Run(async () =>
                        {
                            if (Logger.IsEnabled(LogLevel.Information))
                            {
                                Logger.LogInformation($"[Http RequestTraceIdentifier]:{context.Request.RequestTraceIdentifier.ToString()}-{context.Request.RemoteEndPoint.ToString()}-{context.Request.RawUrl}");
                            }
                            if (context.Request.RawUrl.StartsWith("/favicon.ico"))
                            {
                                context.Http404();
                                return;
                            }
                            if (context.TryGetAVInfo(out JT1078AVInfo jT1078AVInfo))
                            {
                                await context.Http400();
                                return;
                            }
                            if (context.Request.RawUrl.Contains(".m3u8"))
                            {
                                ProcessM3u8(context, jT1078AVInfo);
                            }
                            else if (context.Request.RawUrl.Contains(".ts")) 
                            {
                                ProcessTs(context, jT1078AVInfo);
                            }
                            else if (context.Request.RawUrl.Contains(".flv"))
                            {
                                ProcessFlv(context, jT1078AVInfo);
                            }
                            else if (context.Request.RawUrl.Contains(".mp4"))
                            {
                                ProcessFMp4(context, jT1078AVInfo);
                            }
                            else
                            {
                                await context.Http401();
                            }
                        });
                    }
                    catch (Exception ex)
                    {
                        await context.Http500();
                        Logger.LogError(ex, $"[Http RequestTraceIdentifier]:{context.Request.RequestTraceIdentifier.ToString()}-{context.Request.RemoteEndPoint.ToString()}-{context.Request.RawUrl}-{ex.StackTrace}");
                    }
                }
            }, cancellationToken);
            return Task.CompletedTask;
        }

        private void ProcessM3u8(HttpListenerContext context, JT1078AVInfo jT1078AVInfo)
        {
            if (authorization.Authorization(context, out IPrincipal principal))
            {
                hLSRequestManager.HandleHlsRequest(context, principal);
            }
        }

        private void ProcessTs(HttpListenerContext context, JT1078AVInfo jT1078AVInfo)
        {
            //ts 无需验证
            hLSRequestManager.HandleHlsRequest(context, default);
        }

        private async void ProcessFlv(HttpListenerContext context, JT1078AVInfo jT1078AVInfo)
        {
            if (authorization.Authorization(context, out IPrincipal principal))
            {
                if (context.Request.IsWebSocketRequest)
                {
                    await ProccessWebSocket(context, principal, jT1078AVInfo, RTPVideoType.Ws_Flv);
                }
                else
                {
                    ProccessHttpKeepLive(context, principal, jT1078AVInfo, RTPVideoType.Http_Flv);
                }
            }
        }

        private async void ProcessFMp4(HttpListenerContext context, JT1078AVInfo jT1078AVInfo)
        {
            if (authorization.Authorization(context, out IPrincipal principal))
            {
                if (context.Request.IsWebSocketRequest)
                {
                    await ProccessWebSocket(context, principal, jT1078AVInfo, RTPVideoType.Ws_FMp4);
                }
                else
                {
                    ProccessHttpKeepLive(context, principal, jT1078AVInfo, RTPVideoType.Http_FMp4);
                }
            }
        }

        private async ValueTask ProccessWebSocket(HttpListenerContext context, IPrincipal principal, JT1078AVInfo jT1078AVInfo, RTPVideoType videoType)
        {
            HttpListenerWebSocketContext wsContext = await context.AcceptWebSocketAsync(null, keepAliveInterval: TimeSpan.FromSeconds(5));
            var jT1078HttpContext = new JT1078HttpContext(context, wsContext, principal);
            jT1078HttpContext.Sim = jT1078AVInfo.Sim;
            jT1078HttpContext.ChannelNo = jT1078AVInfo.ChannelNo;
            jT1078HttpContext.RTPVideoType = videoType;
            SessionManager.TryAdd(jT1078HttpContext);
            //这个发送出去，flv.js就报错了
            //await jT1078HttpContext.WebSocketSendHelloAsync();
            await Task.Factory.StartNew(async (state) =>
            {
                //https://www.bejson.com/httputil/websocket/
                //ws://localhost:15555?token=22&sim=1221&channel=1
                var websocketContext = state as JT1078HttpContext;
                while (websocketContext.WebSocketContext.WebSocket.State == WebSocketState.Open ||
                        websocketContext.WebSocketContext.WebSocket.State == WebSocketState.Connecting)
                {
                    var buffer = ArrayPool<byte>.Shared.Rent(256);
                    try
                    {
                        //客户端主动断开需要有个线程去接收通知,不然会客户端会卡死直到超时
                        WebSocketReceiveResult receiveResult = await websocketContext.WebSocketContext.WebSocket.ReceiveAsync(buffer, CancellationToken.None);
                        if (receiveResult.EndOfMessage)
                        {
                            if (receiveResult.Count > 0)
                            {
                                var data = buffer.AsSpan().Slice(0, receiveResult.Count).ToArray();
                                if (Logger.IsEnabled(LogLevel.Trace))
                                {
                                    Logger.LogTrace($"[ws receive]:{Encoding.UTF8.GetString(data)}");
                                }
                                await websocketContext.WebSocketSendTextAsync(data);
                            }
                        }
                    }
                    finally
                    {
                        ArrayPool<byte>.Shared.Return(buffer);
                    }
                }
                if (Logger.IsEnabled(LogLevel.Information))
                {
                    Logger.LogInformation($"[ws close]:{websocketContext.SessionId}-{websocketContext.Sim}-{websocketContext.ChannelNo}-{websocketContext.StartTime:yyyyMMddhhmmss}-{websocketContext.Context.Request.RawUrl}");
                }
                SessionManager.TryRemove(websocketContext.SessionId);
            }, jT1078HttpContext);
        }

        private void ProccessHttpKeepLive(HttpListenerContext context, IPrincipal principal, JT1078AVInfo jT1078AVInfo, RTPVideoType videoType)
        {
            var jT1078HttpContext = new JT1078HttpContext(context, principal);
            jT1078HttpContext.RTPVideoType = videoType;
            jT1078HttpContext.Sim = jT1078AVInfo.Sim;
            jT1078HttpContext.ChannelNo = jT1078AVInfo.ChannelNo;
            SessionManager.TryAdd(jT1078HttpContext);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public Task StopAsync(CancellationToken cancellationToken)
        {
            try
            {
                Logger.LogInformation($"JT1078 Http Server stop at {IPAddress.Any}:{Configuration.HttpPort}.");
                SessionManager.TryRemoveAll();
                listener.Stop();
            }
            catch (System.ObjectDisposedException ex)
            {

            }
            catch (Exception ex)
            {
                Logger.LogError(ex, $"JT1078 Http Server error at {IPAddress.Any}:{Configuration.HttpPort}.");
            }
            return Task.CompletedTask;
        }
    }
}
