using JT1078.Gateway.Extensions;
using JT1078.Gateway.Metadata;
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

        public JT1078HttpSessionManager()
        {
            Sessions = new ConcurrentDictionary<string, JT1078HttpContext>();
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

        public void SendHttpChunk(byte[] data)
        {
            //todo:set http chunk
            //todo:session close notice
            //byte[] b = Encoding.UTF8.GetBytes("ack");
            //context.Response.StatusCode = 200;
            //context.Response.KeepAlive = true;
            //context.Response.ContentLength64 = b.Length;
            //await context.Response.OutputStream.WriteAsync(b, 0, b.Length);
            //context.Response.Close();
        }

        public int SessionCount 
        {
            get
            {
                return Sessions.Count;
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
