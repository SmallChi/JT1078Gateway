using System;
using System.Collections.Generic;
using System.Net;
using System.Net.WebSockets;
using System.Security.Principal;
using System.Text;

namespace JT1078.Gateway.Metadata
{
    public class JT1078HttpContext
    {
        public string SessionId { get; }
        public HttpListenerContext Context { get; }
        public HttpListenerWebSocketContext WebSocketContext { get; }
        public IPrincipal User { get; }
        public string Sim { get; set; }
        public int ChannelNo { get; set; }
        public bool IsWebSocket
        {
            get
            {
                return Context.Request.IsWebSocketRequest;
            }
        }
        public DateTime StartTime { get; set; }
        public bool FirstSend { get; set; }
        public JT1078HttpContext(HttpListenerContext context, IPrincipal user)
        {
            Context = context;
            User = user;
            StartTime = DateTime.Now;
            SessionId = Guid.NewGuid().ToString("N");
            FirstSend = false;
        }
        public JT1078HttpContext(HttpListenerContext context, HttpListenerWebSocketContext webSocketContext, IPrincipal user)
        {
            Context = context;
            WebSocketContext = webSocketContext;
            User = user;
            StartTime = DateTime.Now;
            SessionId = Guid.NewGuid().ToString("N");
            FirstSend = false;
        }
    }
}
