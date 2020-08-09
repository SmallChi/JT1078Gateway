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
                finally
                {
                    //todo:session close notice
                }
            }
        }

        private void remove(string sessionId)
        {
            if (Sessions.TryRemove(sessionId, out JT1078HttpContext session))
            {
                //todo:session close notice
            }
        }

        /// <summary>
        /// 发送音视频数据
        /// </summary>
        /// <param name="sim"></param>
        /// <param name="channelNo"></param>
        /// <param name="data"></param>
        public void SendAVData(string sim,int channelNo,byte[] data)
        {
            var contexts = Sessions.Select(s => s.Value).Where(w => w.Sim == sim && w.ChannelNo == channelNo).ToList();
            ParallelLoopResult parallelLoopResult= Parallel.ForEach(contexts, async(context) => 
            {
                if (context.IsWebSocket)
                {
                    try
                    {
                        await context.WebSocketSendBinaryAsync(data);
                    }
                    catch (Exception ex)
                    {
                        if (Logger.IsEnabled(LogLevel.Information))
                        {
                            Logger.LogInformation($"[ws close]:{context.SessionId}-{context.Sim}-{context.ChannelNo}-{context.StartTime:yyyyMMddhhmmss}");
                        }
                        remove(context.SessionId);
                    }
                }
                else
                {
                    if (!context.SendChunked)
                    {
                        context.SendChunked = true;
                        Sessions.TryUpdate(context.SessionId, context, context);
                        try
                        {
                            await context.HttpSendFirstChunked(data);
                        }
                        catch (Exception ex)
                        {
                            if (Logger.IsEnabled(LogLevel.Information))
                            {
                                Logger.LogInformation($"[http close]:{context.SessionId}-{context.Sim}-{context.ChannelNo}-{context.StartTime:yyyyMMddhhmmss}");
                            }
                            remove(context.SessionId);
                        }
                    }
                    else
                    {
                        try
                        {
                            await context.HttpSendChunked(data);
                        }
                        catch (Exception ex)
                        {
                            if (Logger.IsEnabled(LogLevel.Information))
                            {
                                Logger.LogInformation($"[http close]:{context.SessionId}-{context.Sim}-{context.ChannelNo}-{context.StartTime:yyyyMMddhhmmss}");
                            }
                            remove(context.SessionId);
                        }
                    }
                }
            });
            if (parallelLoopResult.IsCompleted)
            {

            }
        }

        /// <summary>
        /// 发送音视频数据到websocket
        /// </summary>
        /// <param name="sim"></param>
        /// <param name="channelNo"></param>
        /// <param name="data"></param>
        public void SendAVData2WebSocket(string sim, int channelNo, byte[] data)
        {
            var contexts = Sessions.Select(s => s.Value).Where(w => w.Sim == sim && w.ChannelNo == channelNo && w.IsWebSocket).ToList();
            ParallelLoopResult parallelLoopResult = Parallel.ForEach(contexts, async (context) =>
            {
                if (context.IsWebSocket)
                {
                    try
                    {
                        await context.WebSocketSendBinaryAsync(data);
                    }
                    catch (Exception ex)
                    {
                        if (Logger.IsEnabled(LogLevel.Information))
                        {
                            Logger.LogInformation($"[ws close]:{context.SessionId}-{context.Sim}-{context.ChannelNo}-{context.StartTime:yyyyMMddhhmmss}");
                        }
                        remove(context.SessionId);
                    }
                }
            });
            if (parallelLoopResult.IsCompleted)
            {

            }
        }

        /// <summary>
        /// 发送音视频数据到Http Chunked中
        /// </summary>
        /// <param name="sim"></param>
        /// <param name="channelNo"></param>
        /// <param name="data"></param>
        public void SendAVData2HttpChunked(string sim, int channelNo, byte[] data)
        {
            var contexts = Sessions.Select(s => s.Value).Where(w => w.Sim == sim && w.ChannelNo == channelNo && !w.IsWebSocket).ToList();
            ParallelLoopResult parallelLoopResult = Parallel.ForEach(contexts, async (context) =>
            {
                if (!context.SendChunked)
                {
                    context.SendChunked = true;
                    Sessions.TryUpdate(context.SessionId, context, context);
                    try
                    {
                        await context.HttpSendFirstChunked(data);
                    }
                    catch (Exception ex)
                    {
                        if (Logger.IsEnabled(LogLevel.Information))
                        {
                            Logger.LogInformation($"[http close]:{context.SessionId}-{context.Sim}-{context.ChannelNo}-{context.StartTime:yyyyMMddhhmmss}");
                        }
                        remove(context.SessionId);
                    }
                }
                else
                {
                    try
                    {
                        await context.HttpSendChunked(data);
                    }
                    catch (Exception ex)
                    {
                        if (Logger.IsEnabled(LogLevel.Information))
                        {
                            Logger.LogInformation($"[http close]:{context.SessionId}-{context.Sim}-{context.ChannelNo}-{context.StartTime:yyyyMMddhhmmss}");
                        }
                        remove(context.SessionId);
                    }
                }             
            });
            if (parallelLoopResult.IsCompleted)
            {

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
