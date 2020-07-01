using JT1078.Gateway.Abstractions.Enums;
using JT1078.Gateway.Abstractions;
using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace JT1078.Gateway.Sessions
{
    public class JT1078UdpSession: IJT1078Session
    {
        public JT1078UdpSession(Socket socket)
        {
            ActiveTime = DateTime.Now;
            StartTime = DateTime.Now;
            SessionID = Guid.NewGuid().ToString("N");
            ReceiveTimeout = new CancellationTokenSource();
            Client = socket;
        }

        /// <summary>
        /// 终端手机号
        /// </summary>
        public string TerminalPhoneNo { get; set; }
        public DateTime ActiveTime { get; set; }
        public DateTime StartTime { get; set; }
        public JT1078TransportProtocolType TransportProtocolType { get; set; } = JT1078TransportProtocolType.udp;
        public string SessionID { get; }
        public Socket Client { get; set; }
        public CancellationTokenSource ReceiveTimeout { get; set; }
        public EndPoint RemoteEndPoint { get; set ; }
        public void Close()
        {
            
        }
    }
}
