using JT1078.Gateway.Abstractions;
using JT1078.Gateway.Abstractions.Enums;
using JT1078.Gateway.Services;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace JT1078.Gateway.Sessions
{
    /// <summary>
    /// 
    /// <remark>不支持变态类型:既发TCP和UDP</remark>
    /// </summary>
    public class JT1078SessionManager
    {
        private readonly ILogger logger;
        public ConcurrentDictionary<string, IJT1078Session> Sessions { get; }
        public ConcurrentDictionary<string, IJT1078Session> TerminalPhoneNoSessions { get; }
        private readonly JT1078SessionNoticeService SessionNoticeService;
        public JT1078SessionManager(
            JT1078SessionNoticeService jT1078SessionNoticeService,
            ILoggerFactory loggerFactory)
        {
            Sessions = new ConcurrentDictionary<string, IJT1078Session>(StringComparer.OrdinalIgnoreCase);
            TerminalPhoneNoSessions = new ConcurrentDictionary<string, IJT1078Session>(StringComparer.OrdinalIgnoreCase);
            logger = loggerFactory.CreateLogger<JT1078SessionManager>();
            this.SessionNoticeService = jT1078SessionNoticeService;
        }

        public int TotalSessionCount
        {
            get
            {
                return Sessions.Count;
            }
        }

        public int TcpSessionCount
        {
            get
            {
                return Sessions.Where(w => w.Value.TransportProtocolType == JT1078TransportProtocolType.tcp).Count();
            }
        }

        public int UdpSessionCount
        {
            get
            {
                return Sessions.Where(w => w.Value.TransportProtocolType == JT1078TransportProtocolType.udp).Count();
            }
        }

        internal void TryLink(string terminalPhoneNo, IJT1078Session session)
        {
            DateTime curretDatetime= DateTime.Now;
            if (TerminalPhoneNoSessions.TryGetValue(terminalPhoneNo,out IJT1078Session cacheSession))
            {
                if (session.SessionID != cacheSession.SessionID)
                {
                    //从转发到直连的数据需要更新缓存
                    session.ActiveTime = curretDatetime;
                    TerminalPhoneNoSessions.TryUpdate(terminalPhoneNo, session, cacheSession);
                    //会话通知
                    SessionNoticeService.SessionNoticeBlockingCollection.Add((JT1078GatewayConstants.SessionOnline, terminalPhoneNo, session.TransportProtocolType.ToString()));
                }
                else
                {
                    cacheSession.ActiveTime = curretDatetime;
                    TerminalPhoneNoSessions.TryUpdate(terminalPhoneNo, cacheSession, cacheSession);
                }
            }
            else
            {
                session.TerminalPhoneNo = terminalPhoneNo;
                if (TerminalPhoneNoSessions.TryAdd(terminalPhoneNo, session))
                {
                    //会话通知
                    SessionNoticeService.SessionNoticeBlockingCollection.Add((JT1078GatewayConstants.SessionOnline, terminalPhoneNo, session.TransportProtocolType.ToString()));
                }
            }
        }

        public IJT1078Session TryLink(string terminalPhoneNo, Socket socket, EndPoint remoteEndPoint)
        {
            if (TerminalPhoneNoSessions.TryGetValue(terminalPhoneNo, out IJT1078Session currentSession))
            {
                currentSession.ActiveTime = DateTime.Now;
                currentSession.TerminalPhoneNo = terminalPhoneNo;
                currentSession.RemoteEndPoint = remoteEndPoint;         
                TerminalPhoneNoSessions.TryUpdate(terminalPhoneNo, currentSession, currentSession);
            }
            else
            {
                JT1078UdpSession session = new JT1078UdpSession(socket);
                session.TerminalPhoneNo = terminalPhoneNo;
                session.RemoteEndPoint = remoteEndPoint;
                Sessions.TryAdd(session.SessionID, session);
                TerminalPhoneNoSessions.TryAdd(terminalPhoneNo, session);
                currentSession = session;
            }
            //会话通知
            SessionNoticeService.SessionNoticeBlockingCollection.Add((JT1078GatewayConstants.SessionOnline, terminalPhoneNo, currentSession.TransportProtocolType.ToString()));
            return currentSession;
        }

        internal bool TryAdd(IJT1078Session session)
        {
            return Sessions.TryAdd(session.SessionID, session);
        }

        public async ValueTask<bool> TrySendByTerminalPhoneNoAsync(string terminalPhoneNo, byte[] data)
        {
            if(TerminalPhoneNoSessions.TryGetValue(terminalPhoneNo,out var session))
            {
                if (session.TransportProtocolType == JT1078TransportProtocolType.tcp)
                {
                    await session.Client.SendAsync(data, SocketFlags.None);
                }
                else
                {
                    await session.Client.SendToAsync(data, SocketFlags.None, session.RemoteEndPoint);
                }
                return true;
            }
            else
            {
                return false;
            }
        }

        public async ValueTask<bool> TrySendBySessionIdAsync(string sessionId, byte[] data)
        {
            if (Sessions.TryGetValue(sessionId, out var session))
            {
                if(session.TransportProtocolType== JT1078TransportProtocolType.tcp)
                {
                    await session.Client.SendAsync(data, SocketFlags.None);
                }
                else
                {
                    await session.Client.SendToAsync(data, SocketFlags.None, session.RemoteEndPoint);
                }
                return true;
            }
            else
            {
                return false;
            }
        }

        public void RemoveByTerminalPhoneNo(string terminalPhoneNo)
        {
            if (TerminalPhoneNoSessions.TryGetValue(terminalPhoneNo, out var removeTerminalPhoneNoSessions))
            {
                // 处理转发过来的是数据 这时候通道对设备是1对多关系,需要清理垃圾数据
                //1.用当前会话的通道Id找出通过转发过来的其他设备的终端号
                var terminalPhoneNos = TerminalPhoneNoSessions.Where(w => w.Value.SessionID == removeTerminalPhoneNoSessions.SessionID).Select(s => s.Key).ToList();
                //2.存在则一个个移除 
                string tmpTerminalPhoneNo = terminalPhoneNo;
                if (terminalPhoneNos.Count > 0)
                {
                    //3.移除包括当前的设备号
                    foreach (var item in terminalPhoneNos)
                    {
                        TerminalPhoneNoSessions.TryRemove(item, out _);
                    }
                    tmpTerminalPhoneNo = string.Join(",", terminalPhoneNos);
                }
                if (Sessions.TryRemove(removeTerminalPhoneNoSessions.SessionID, out var removeSession))
                {
                    removeSession.Close();
                    if (logger.IsEnabled(LogLevel.Information))
                        logger.LogInformation($"[Session Remove]:{terminalPhoneNo}-{tmpTerminalPhoneNo}");
                    //会话通知
                    SessionNoticeService.SessionNoticeBlockingCollection.Add((JT1078GatewayConstants.SessionOffline, terminalPhoneNo, removeTerminalPhoneNoSessions.TransportProtocolType.ToString()));
                }
            }
        }

        public void RemoveBySessionId(string sessionId)
        {
            if (Sessions.TryRemove(sessionId, out var removeSession))
            {
                var terminalPhoneNos = TerminalPhoneNoSessions.Where(w => w.Value.SessionID == sessionId).Select(s => s.Key).ToList();
                if (terminalPhoneNos.Count > 0)
                {
                    foreach (var item in terminalPhoneNos)
                    {
                        TerminalPhoneNoSessions.TryRemove(item, out _);
                    }
                    var tmpTerminalPhoneNo = string.Join(",", terminalPhoneNos);
                    //会话通知
                    SessionNoticeService.SessionNoticeBlockingCollection.Add((JT1078GatewayConstants.SessionOffline, tmpTerminalPhoneNo, removeSession.TransportProtocolType.ToString()));
                    if (logger.IsEnabled(LogLevel.Information))
                        logger.LogInformation($"[Session Remove]:{tmpTerminalPhoneNo}");
                }
                removeSession.Close();
            }
        }

        public List<JT1078TcpSession> GetTcpAll()
        {
            return TerminalPhoneNoSessions.Where(w => w.Value.TransportProtocolType == JT1078TransportProtocolType.tcp).Select(s => (JT1078TcpSession)s.Value).ToList();
        }

        public List<JT1078UdpSession> GetUdpAll()
        {
            return TerminalPhoneNoSessions.Where(w => w.Value.TransportProtocolType == JT1078TransportProtocolType.udp).Select(s => (JT1078UdpSession)s.Value).ToList();
        }
    }
}
