using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using DotNetty.Transport.Channels;
using Microsoft.Extensions.Options;
using System.Net;
using JT1078.DotNetty.Core.Configurations;
using JT1078.DotNetty.Core.Metadata;

namespace JT1078.DotNetty.Core.Session
{
    /// <summary>
    /// JT1078 udp会话管理
    /// 估计要轮询下
    /// </summary>
    public class JT1078UdpSessionManager
    {
        private readonly ILogger<JT1078UdpSessionManager> logger;

        public JT1078UdpSessionManager(
            ILoggerFactory loggerFactory)
        {
            logger = loggerFactory.CreateLogger<JT1078UdpSessionManager>();
        }

        private ConcurrentDictionary<string, JT1078UdpSession> SessionIdDict = new ConcurrentDictionary<string, JT1078UdpSession>(StringComparer.OrdinalIgnoreCase);

        public int SessionCount
        {
            get
            {
                return SessionIdDict.Count;
            }
        }

        public JT1078UdpSession GetSession(string terminalPhoneNo)
        {
            if (string.IsNullOrEmpty(terminalPhoneNo))
                return default;
            if (SessionIdDict.TryGetValue(terminalPhoneNo, out JT1078UdpSession targetSession))
            {
                return targetSession;
            }
            else
            {
                return default;
            }
        }

        public void TryAdd(IChannel channel,EndPoint sender,string terminalPhoneNo)
        {
            //1.先判断是否在缓存里面
            if (SessionIdDict.TryGetValue(terminalPhoneNo, out JT1078UdpSession UdpSession))
            {
                UdpSession.LastActiveTime=DateTime.Now;
                UdpSession.Sender = sender;
                UdpSession.Channel = channel;
                SessionIdDict.TryUpdate(terminalPhoneNo, UdpSession, UdpSession);
            }
            else
            {
                SessionIdDict.TryAdd(terminalPhoneNo, new JT1078UdpSession(channel, sender, terminalPhoneNo));
            }
        }

        public void Heartbeat(string terminalPhoneNo)
        {
            if (string.IsNullOrEmpty(terminalPhoneNo)) return;
            if (SessionIdDict.TryGetValue(terminalPhoneNo, out JT1078UdpSession oldSession))
            {
                oldSession.LastActiveTime = DateTime.Now;
                SessionIdDict.TryUpdate(terminalPhoneNo, oldSession, oldSession);
            }
        }

        public JT1078UdpSession RemoveSession(string terminalPhoneNo)
        {
            //设备离线可以进行通知
            //使用Redis 发布订阅
            if (string.IsNullOrEmpty(terminalPhoneNo)) return default;
            if (SessionIdDict.TryRemove(terminalPhoneNo, out JT1078UdpSession SessionRemove))
            {
                logger.LogInformation($">>>{terminalPhoneNo} Session Remove.");
                return SessionRemove;
            }
            else
            {
                return default;
            }
        }

        public void RemoveSessionByChannel(IChannel channel)
        {
            //设备离线可以进行通知
            //使用Redis 发布订阅
            var terminalPhoneNos = SessionIdDict.Where(w => w.Value.Channel.Id == channel.Id).Select(s => s.Key).ToList();
            if (terminalPhoneNos.Count > 0)
            {
                foreach (var key in terminalPhoneNos)
                {
                    SessionIdDict.TryRemove(key, out JT1078UdpSession SessionRemove);
                }
                string nos = string.Join(",", terminalPhoneNos);
                logger.LogInformation($">>>{nos} Channel Remove.");
            }        
        }

        public IEnumerable<JT1078UdpSession> GetAll()
        {
            return SessionIdDict.Select(s => s.Value).ToList();
        }
    }
}

