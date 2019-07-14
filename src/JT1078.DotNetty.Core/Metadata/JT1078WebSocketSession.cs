using DotNetty.Transport.Channels;
using System;
using System.Net;

namespace JT1078.DotNetty.Core.Metadata
{
    public class JT1078WebSocketSession
    {
        public JT1078WebSocketSession(
            IChannel channel,
            string userId)
        {
            Channel = channel;
            UserId = userId;
            StartTime = DateTime.Now;
            LastActiveTime = DateTime.Now;
        }

        public JT1078WebSocketSession() { }

        public string UserId { get; set; }

        public string AttachInfo { get; set; }

        public IChannel Channel { get; set; }

        public DateTime LastActiveTime { get; set; }

        public DateTime StartTime { get; set; }
    }
}
