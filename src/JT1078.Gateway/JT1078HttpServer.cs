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

namespace JT1078.Gateway
{
    public class JT1078HttpServer : IHostedService
    {
        private readonly ILogger Logger;

        private readonly JT1078Configuration Configuration;

        private readonly IJT1078Authorization authorization;

        private HttpListener listener;

        private JT1078HttpSessionManager SessionManager;

        public JT1078HttpServer(
            IOptions<JT1078Configuration> jT1078ConfigurationAccessor,
            IJT1078Authorization authorization,
            JT1078HttpSessionManager sessionManager,
            ILoggerFactory loggerFactory)
        {
            Logger = loggerFactory.CreateLogger<JT1078TcpServer>();
            Configuration = jT1078ConfigurationAccessor.Value;
            this.authorization = authorization;
            this.SessionManager = sessionManager;
        }

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
            }
            Logger.LogInformation($"JT1078 Http Server start at {IPAddress.Any}:{Configuration.HttpPort}.");
            Task.Factory.StartNew(async() => 
            {
                while (listener.IsListening)
                {
                    var context = await listener.GetContextAsync();
                    try
                    {
                        if (authorization.Authorization(context,out var principal))
                        {
                            await ProcessRequestAsync(context, principal);
                        }
                        else
                        {
                            await context.Http401();
                        }
                    }
                    catch (Exception ex)
                    {
                        await context.Http500();
                        Logger.LogError(ex, ex.StackTrace);
                    }
                }
            }, cancellationToken);
            return Task.CompletedTask;
        }

        private async ValueTask ProcessRequestAsync(HttpListenerContext context, IPrincipal principal)
        {
            if(context.Request.RawUrl.StartsWith("/favicon.ico"))
            {
                context.Http404();
            }
            //todo:.m3u8 .ts
            if (Logger.IsEnabled(LogLevel.Trace))
            {
                Logger.LogTrace($"[http RequestTraceIdentifier]:{context.Request.RequestTraceIdentifier.ToString()}-{context.Request.RemoteEndPoint.ToString()}");
            }
            string sim = context.Request.QueryString.Get("sim");
            string channel = context.Request.QueryString.Get("channel");
            if(string.IsNullOrEmpty(sim) || string.IsNullOrEmpty(channel))
            {
                await context.Http400();
                return;
            }
            int.TryParse(channel, out int channelNo);
            if (context.Request.IsWebSocketRequest)
            {
                HttpListenerWebSocketContext wsContext = await context.AcceptWebSocketAsync(null);
                var jT1078HttpContext = new JT1078HttpContext(context, wsContext,principal);
                jT1078HttpContext.Sim = sim;
                jT1078HttpContext.ChannelNo = channelNo;
                SessionManager.TryAdd(jT1078HttpContext);
                await wsContext.WebSocket.SendAsync(Encoding.UTF8.GetBytes("hello,jt1078"), WebSocketMessageType.Text, true, CancellationToken.None);
                await Task.Factory.StartNew(async(state) => 
                {
                    //https://www.bejson.com/httputil/websocket/
                    //ws://localhost:15555?token=22&sim=1221&channel=1
                    var websocketContext = state as JT1078HttpContext;
                    while(websocketContext.WebSocketContext.WebSocket.State == WebSocketState.Open || 
                          websocketContext.WebSocketContext.WebSocket.State == WebSocketState.Connecting)
                    {
                        var buffer = ArrayPool<byte>.Shared.Rent(256);
                        try
                        {
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
                    if (Logger.IsEnabled(LogLevel.Trace))
                    {
                        Logger.LogTrace($"[ws close]:{websocketContext}");
                    }
                    SessionManager.TryRemove(websocketContext.SessionId);
                }, jT1078HttpContext);
            }
            else
            {
                var jT1078HttpContext = new JT1078HttpContext(context,principal);
                jT1078HttpContext.Sim = sim;
                jT1078HttpContext.ChannelNo = channelNo;
                SessionManager.TryAdd(jT1078HttpContext);
            }
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            try
            {
                SessionManager.TryRemoveAll();
                listener.Stop();
            }
            catch (System.ObjectDisposedException ex)
            {

            }
            catch (Exception ex)
            {

            }
            return Task.CompletedTask;
        }
    }
}
