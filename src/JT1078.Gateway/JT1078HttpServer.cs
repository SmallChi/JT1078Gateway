using JT1078.Gateway.Abstractions;
using JT1078.Gateway.Configurations;
using JT1078.Gateway.Metadata;
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

namespace JT1078.Gateway
{
    public class JT1078HttpServer : IHostedService
    {
        private readonly ILogger Logger;

        private readonly JT1078Configuration Configuration;

        private readonly IJT1078Authorization authorization;

        private HttpListener listener;

        public JT1078HttpServer(
            IOptions<JT1078Configuration> jT1078ConfigurationAccessor,
            IJT1078Authorization authorization,
            ILoggerFactory loggerFactory)
        {
            Logger = loggerFactory.CreateLogger<JT1078TcpServer>();
            Configuration = jT1078ConfigurationAccessor.Value;
            this.authorization = authorization;
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
            listener.Prefixes.Add($"http://*:{Configuration.HttpPort}/");
            listener.Start();
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
                            await Http401(context);
                        }
                    }
                    catch (Exception ex)
                    {
                        await Http500(context);
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
                Http404(context);
            }
            if (Logger.IsEnabled(LogLevel.Trace))
            {
                Logger.LogTrace($"[http RequestTraceIdentifier]:{context.Request.RequestTraceIdentifier.ToString()}-{context.Request.RemoteEndPoint.ToString()}");
            }
            string sim = context.Request.QueryString.Get("sim");
            string channel = context.Request.QueryString.Get("channel");
            if(string.IsNullOrEmpty(sim) || string.IsNullOrEmpty(channel))
            {
                await Http400(context);
                return;
            }
            int.TryParse(channel, out int channelNo);
            if (context.Request.IsWebSocketRequest)
            {
                HttpListenerWebSocketContext wsContext = await context.AcceptWebSocketAsync(null);
                var jT1078HttpContext = new JT1078HttpContext(context, wsContext,principal);
                jT1078HttpContext.Sim = sim;
                jT1078HttpContext.ChannelNo = channelNo;
                //todo: add session manager
                await wsContext.WebSocket.SendAsync(Encoding.UTF8.GetBytes("hello,jt1078"), WebSocketMessageType.Text, true, CancellationToken.None);
                await Task.Factory.StartNew(async(state) => 
                {
                    //https://www.bejson.com/httputil/websocket/
                    //ws://127.0.0.1:15555?token=22&sim=1221&channel=1
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
                                    await websocketContext.WebSocketContext.WebSocket.SendAsync(data, WebSocketMessageType.Text, true, CancellationToken.None);
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
                    //todo:session close notice
                    await websocketContext.WebSocketContext.WebSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "normal", CancellationToken.None);
                }, jT1078HttpContext);
            }
            else
            {
                var jT1078HttpContext = new JT1078HttpContext(context,principal);
                jT1078HttpContext.Sim = sim;
                jT1078HttpContext.ChannelNo = channelNo;

                //todo:add session manager

                //todo:set http chunk

                //todo:session close notice

                byte[] b = Encoding.UTF8.GetBytes("ack");
                context.Response.StatusCode = 200;
                context.Response.KeepAlive = true;
                context.Response.ContentLength64 = b.Length;
                await context.Response.OutputStream.WriteAsync(b, 0, b.Length);
                context.Response.Close();
            }
        }

        private async ValueTask Http401(HttpListenerContext context)
        {
            byte[] b = Encoding.UTF8.GetBytes("auth error");
            context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
            context.Response.KeepAlive = false;
            context.Response.ContentLength64 = b.Length;
            var output = context.Response.OutputStream;
            await output.WriteAsync(b, 0, b.Length);
            context.Response.Close();
        }

        private async ValueTask Http400(HttpListenerContext context)
        {
            byte[] b = Encoding.UTF8.GetBytes($"sim and channel parameter required.");
            context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
            context.Response.KeepAlive = false;
            context.Response.ContentLength64 = b.Length;
            var output = context.Response.OutputStream;
            await output.WriteAsync(b, 0, b.Length);
            context.Response.Close();
        }
        
        private void Http404(HttpListenerContext context)
        {
            context.Response.StatusCode = (int)HttpStatusCode.NotFound;
            context.Response.KeepAlive = false;
            context.Response.Close();
        }

        private async ValueTask Http500(HttpListenerContext context)
        {
            byte[] b = Encoding.UTF8.GetBytes("inner error");
            context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
            context.Response.KeepAlive = false;
            context.Response.ContentLength64 = b.Length;
            var output = context.Response.OutputStream;
            await output.WriteAsync(b, 0, b.Length);
            context.Response.Close();
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            try
            {
                listener.Stop();
            }
            catch (System.ObjectDisposedException ex)
            {

            }
            return Task.CompletedTask;
        }
    }
}
