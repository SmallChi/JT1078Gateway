using JT1078.Gateway.Extensions;
using JT1078.Gateway.Metadata;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace JT1078.Gateway.Sessions
{
    public class JT1078HttpSessionManager
    {
        public ConcurrentDictionary<string, JT1078HttpContext> Sessions { get; }
        private ILogger Logger;
        public JT1078HttpSessionManager(ILoggerFactory loggerFactory)
        {
            Sessions = new ConcurrentDictionary<string, JT1078HttpContext>();
            Logger = loggerFactory.CreateLogger<JT1078HttpSessionManager>();
        }

        public bool TryAdd(JT1078HttpContext  httpContext)
        {
     
            return Sessions.TryAdd(httpContext.SessionId, httpContext);
        }

        public void AddOrUpdate(JT1078HttpContext httpContext) {
            var session = Sessions.FirstOrDefault(m => m.Value.Sim == httpContext.Sim && m.Value.ChannelNo == httpContext.ChannelNo);
            if (string.IsNullOrEmpty(session.Key))
            {
                Sessions.TryAdd(httpContext.SessionId, httpContext);
            }
            else {
                Sessions.TryUpdate(session.Key, httpContext, session.Value);
            }
        }

        public async void TryRemove(string sessionId)
        {
            if(Sessions.TryRemove(sessionId, out JT1078HttpContext session))
            {
                try
                {
                    if (session.IsWebSocket)
                    {
                        await session.WebSocketClose("close");
                    }
                    else
                    {
              
                        await session.HttpClose();
                    }
                }
                catch (Exception)
                {

                }
            }
        }

        public async void TryRemoveBySim(string sim)
        {
            var keys=Sessions.Where(f => f.Value.Sim == sim).Select(s => s.Key);
            foreach(var key in keys)
            {
                if (Sessions.TryRemove(key, out JT1078HttpContext session))
                {
                    try
                    {
                        if (session.IsWebSocket)
                        {
                            await session.WebSocketClose("close");
                        }
                        else
                        {

                            await session.HttpClose();
                        }
                    }
                    catch (Exception)
                    {

                    }
                }
            }  
        }

        private void remove(string sessionId)
        {
            if (Sessions.TryRemove(sessionId, out JT1078HttpContext session))
            {
            }
        }

        /// <summary>
        /// 发送音视频数据
        /// </summary>
        /// <param name="httpContext"></param>
        /// <param name="data"></param>
        /// <param name="firstSend"></param>
        public async void SendAVData(JT1078HttpContext httpContext, byte[] data, bool firstSend)
        {
            if (httpContext.IsWebSocket)
            {
                if (firstSend)
                {
                    httpContext.FirstSend = firstSend;
                    Sessions.TryUpdate(httpContext.SessionId, httpContext, httpContext);
                }
                try
                {
                    await httpContext.WebSocketSendBinaryAsync(data);
                }
                catch (Exception ex)
                {
                    if (Logger.IsEnabled(LogLevel.Information))
                    {
                        Logger.LogInformation($"[ws close]:{httpContext.SessionId}-{httpContext.Sim}-{httpContext.ChannelNo}-{httpContext.StartTime:yyyyMMddhhmmss}");
                    }
                    remove(httpContext.SessionId);
                }                   
            }
            else
            {
                if (firstSend)
                {
                    httpContext.FirstSend = firstSend;
                    Sessions.TryUpdate(httpContext.SessionId, httpContext, httpContext);
                    try
                    {
                        await httpContext.HttpSendFirstChunked(data);
                    }
                    catch (Exception ex)
                    {
                        if (Logger.IsEnabled(LogLevel.Information))
                        {
                            Logger.LogInformation($"[http close]:{httpContext.SessionId}-{httpContext.Sim}-{httpContext.ChannelNo}-{httpContext.StartTime:yyyyMMddhhmmss}");
                        }
                        remove(httpContext.SessionId);
                    }
                }
                else
                {
                    try
                    {
                        await httpContext.HttpSendChunked(data);
                    }
                    catch (Exception ex)
                    {
                        if (Logger.IsEnabled(LogLevel.Information))
                        {
                            Logger.LogInformation($"[http close]:{httpContext.SessionId}-{httpContext.Sim}-{httpContext.ChannelNo}-{httpContext.StartTime:yyyyMMddhhmmss}");
                        }
                        remove(httpContext.SessionId);
                    }
                }
            }
        }

        public int SessionCount 
        {
            get
            {
                return Sessions.Count;
            } 
        }

        public int HttpSessionCount
        {
            get
            {
                return Sessions.Count(c=>!c.Value.IsWebSocket);
            }
        }

        public int WebSocketSessionCount
        {
            get
            {
                return Sessions.Count(c => c.Value.IsWebSocket);
            }
        }

        public List<JT1078HttpContext> GetAllBySimAndChannelNo(string sim, int channelNo)
        {
            return Sessions.Select(s => s.Value).Where(w => w.Sim == sim && w.ChannelNo == channelNo).ToList();
        }

        public List<JT1078HttpContext> GetAllHttpContextBySimAndChannelNo(string sim, int channelNo)
        {
            return Sessions.Select(s => s.Value).Where(w => w.Sim == sim && w.ChannelNo == channelNo&&!w.IsWebSocket).ToList();
        }

        public List<JT1078HttpContext> GetAll()
        {
            return Sessions.Select(s => s.Value).ToList();
        }

        internal void TryRemoveAll()
        {
            foreach(var item in Sessions)
            {
                try
                {
                    if (item.Value.IsWebSocket)
                    {
                        item.Value.WebSocketContext.WebSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "server close", CancellationToken.None);
                    }
                    else
                    {
                        item.Value.Context.Response.Close();
                    }
                }
                catch (Exception)
                {

                }
            }
        }
    }
}
