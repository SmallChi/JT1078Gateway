using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using DotNetty.Transport.Channels;
using JT1078.Gateway.Metadata;

namespace JT1078.Gateway.Session
{
    /// <summary>
    /// JT1078 Tcp会话管理
    /// </summary>
    public class JT1078TcpSessionManager
    {
        private readonly ILogger<JT1078TcpSessionManager> logger;

        public JT1078TcpSessionManager(
            ILoggerFactory loggerFactory)
        {
            logger = loggerFactory.CreateLogger<JT1078TcpSessionManager>();
        }

        private ConcurrentDictionary<string, JT1078TcpSession> SessionIdDict = new ConcurrentDictionary<string, JT1078TcpSession>(StringComparer.OrdinalIgnoreCase);

        public int SessionCount
        {
            get
            {
                return SessionIdDict.Count;
            }
        }

        public JT1078TcpSession GetSession(string terminalPhoneNo)
        {
            if (string.IsNullOrEmpty(terminalPhoneNo))
                return default;
            if (SessionIdDict.TryGetValue(terminalPhoneNo, out JT1078TcpSession targetSession))
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
            if (SessionIdDict.TryGetValue(terminalPhoneNo, out JT1078TcpSession oldSession))
            {
                oldSession.LastActiveTime = DateTime.Now;
                oldSession.Channel = channel;
                SessionIdDict.TryUpdate(terminalPhoneNo, oldSession, oldSession);
            }
            else
            {
                JT1078TcpSession session = new JT1078TcpSession(channel, terminalPhoneNo);
                if (SessionIdDict.TryAdd(terminalPhoneNo, session))
                {

                }
            }
        }

        public JT1078TcpSession RemoveSession(string terminalPhoneNo)
        {
            if (string.IsNullOrEmpty(terminalPhoneNo)) return default;
            if (SessionIdDict.TryRemove(terminalPhoneNo, out JT1078TcpSession sessionRemove))
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
                    SessionIdDict.TryRemove(key, out JT1078TcpSession sessionRemove);
                }
                string nos = string.Join(",", terminalPhoneNos);
                logger.LogInformation($">>>{nos} Channel Remove.");
            }      
        }

        public IEnumerable<JT1078TcpSession> GetAll()
        {
            return SessionIdDict.Select(s => s.Value).ToList();
        }
    }
}

