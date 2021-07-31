using JT1078.FMp4;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.WebSockets;
using System.Security.Principal;
using System.Text;
using System.Text.Json.Serialization;

namespace JT1078.Gateway.Metadata
{
    /// <summary>
    /// http上下文
    /// </summary>
    public class JT1078HttpContext
    {
        /// <summary>
        /// 会话Id
        /// </summary>
        public string SessionId { get; }
        /// <summary>
        /// http上下文
        /// </summary>
        [JsonIgnore]
        public HttpListenerContext Context { get; }
        /// <summary>
        /// ws上下文
        /// </summary>
        [JsonIgnore]
        public HttpListenerWebSocketContext WebSocketContext { get; }
        /// <summary>
        /// 用户信息
        /// </summary>
        public IPrincipal User { get; }
        /// <summary>
        /// 观看视频类型
        /// </summary>
        public RTPVideoType RTPVideoType { get; set; }
        public string Sim { get; set; }
        public int ChannelNo { get; set; }
        /// <summary>
        /// 是否是ws协议
        /// </summary>
        public bool IsWebSocket
        {
            get
            {
                return Context.Request.IsWebSocketRequest;
            }
        }
        /// <summary>
        /// 开始时间
        /// </summary>
        public DateTime StartTime { get; set; }
        /// <summary>
        /// 是否发送首包视频数据
        /// </summary>
        public bool FirstSend { get; set; }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="context"></param>
        /// <param name="user"></param>
        public JT1078HttpContext(HttpListenerContext context, IPrincipal user)
        {
            Context = context;
            User = user;
            StartTime = DateTime.Now;
            SessionId = Guid.NewGuid().ToString("N");
            FirstSend = false;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="context"></param>
        /// <param name="webSocketContext"></param>
        /// <param name="user"></param>
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
    /// <summary>
    /// 观看视频类型
    /// </summary>
    public enum RTPVideoType
    {
        /// <summary>
        /// Http_Flv
        /// </summary>
        Http_Flv,
        /// <summary>
        /// Ws_Flv
        /// </summary>
        Ws_Flv,
        /// <summary>
        /// Http_Hls
        /// </summary>
        Http_Hls,
        /// <summary>
        /// Http_FMp4
        /// </summary>
        Http_FMp4,
        /// <summary>
        /// Ws_FMp4
        /// </summary>
        Ws_FMp4,
    }
}
