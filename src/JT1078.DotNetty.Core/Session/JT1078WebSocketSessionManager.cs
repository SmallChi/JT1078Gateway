using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using DotNetty.Transport.Channels;
using JT1078.DotNetty.Core.Metadata;

namespace JT1078.DotNetty.Core.Session
{
    /// <summary>
    /// JT1078 WebSocket会话管理
    /// </summary>
    public class JT1078WebSocketSessionManager
    {
        private readonly ILogger<JT1078WebSocketSessionManager> logger;

        public JT1078WebSocketSessionManager(
            ILoggerFactory loggerFactory)
        {
            logger = loggerFactory.CreateLogger<JT1078WebSocketSessionManager>();
        }

        private ConcurrentDictionary<string, JT1078WebSocketSession> SessionDict = new ConcurrentDictionary<string,JT1078WebSocketSession>();

        public int SessionCount
        {
            get
            {
                return SessionDict.Count;
            }
        }

        public List<JT1078WebSocketSession> GetSessions(string userId)
        {
           return SessionDict.Where(m => m.Value.UserId == userId).Select(m=>m.Value).ToList();
        }

        public void TryAdd(string userId,IChannel channel)
        {
            SessionDict.TryAdd(channel.Id.AsShortText(), new JT1078WebSocketSession(channel, userId));
            if (logger.IsEnabled(LogLevel.Information))
            {
                logger.LogInformation($">>>{userId},{channel.Id.AsShortText()} Channel Connection.");
            }
        }

        public void RemoveSessionByChannel(IChannel channel)
        {
            if (channel.Open&& SessionDict.TryRemove(channel.Id.AsShortText(), out var session))
            {
                if (logger.IsEnabled(LogLevel.Information))
                {
                    logger.LogInformation($">>>{session.UserId},{session.Channel.Id.AsShortText()} Channel Remove.");
                }
            }
        }
        public IEnumerable<JT1078WebSocketSession> GetAll()
        {
            return SessionDict.Select(s => s.Value).ToList();
        }
    }
}

