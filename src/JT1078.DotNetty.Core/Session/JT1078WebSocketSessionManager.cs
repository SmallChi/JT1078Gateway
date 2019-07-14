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

        private ConcurrentDictionary<string, JT1078WebSocketSession> SessionIdDict = new ConcurrentDictionary<string, JT1078WebSocketSession>(StringComparer.OrdinalIgnoreCase);

        public int SessionCount
        {
            get
            {
                return SessionIdDict.Count;
            }
        }

        public JT1078WebSocketSession GetSession(string userId)
        {
            if (string.IsNullOrEmpty(userId))
                return default;
            if (SessionIdDict.TryGetValue(userId, out JT1078WebSocketSession targetSession))
            {
                return targetSession;
            }
            else
            {
                return default;
            }
        }

        public void TryAdd(string terminalPhoneNo,IChannel channel)
        {
            if (SessionIdDict.TryGetValue(terminalPhoneNo, out JT1078WebSocketSession oldSession))
            {
                oldSession.LastActiveTime = DateTime.Now;
                oldSession.Channel = channel;
                SessionIdDict.TryUpdate(terminalPhoneNo, oldSession, oldSession);
            }
            else
            {
                JT1078WebSocketSession session = new JT1078WebSocketSession(channel, terminalPhoneNo);
                if (SessionIdDict.TryAdd(terminalPhoneNo, session))
                {

                }
            }
        }

        public JT1078WebSocketSession RemoveSession(string terminalPhoneNo)
        {
            if (string.IsNullOrEmpty(terminalPhoneNo)) return default;
            if (SessionIdDict.TryRemove(terminalPhoneNo, out JT1078WebSocketSession sessionRemove))
            {
                logger.LogInformation($">>>{terminalPhoneNo} Session Remove.");
                return sessionRemove;
            }
            else
            {
                return default;
            }  
        }

        public void RemoveSessionByChannel(IChannel channel)
        {
            var terminalPhoneNos = SessionIdDict.Where(w => w.Value.Channel.Id == channel.Id).Select(s => s.Key).ToList();
            if (terminalPhoneNos.Count > 0)
            {
                foreach (var key in terminalPhoneNos)
                {
                    SessionIdDict.TryRemove(key, out JT1078WebSocketSession sessionRemove);
                }
                string nos = string.Join(",", terminalPhoneNos);
                logger.LogInformation($">>>{nos} Channel Remove.");
            }
        }

        public IEnumerable<JT1078WebSocketSession> GetAll()
        {
            return SessionIdDict.Select(s => s.Value).ToList();
        }
    }
}

