using DotNetty.Transport.Channels;
using System;
using System.Net;

namespace JT1078.Gateway.Metadata
{
    public class JT1078UdpSession
    {
        public JT1078UdpSession(IChannel channel,
            EndPoint sender,
            string terminalPhoneNo)
        {
            Channel = channel;
            TerminalPhoneNo = terminalPhoneNo;
            StartTime = DateTime.Now;
            LastActiveTime = DateTime.Now;
            Sender = sender;
        }

        public EndPoint Sender { get; set; }

        public JT1078UdpSession() { }

        /// <summary>
        /// 终端手机号
        /// </summary>
        public string TerminalPhoneNo { get; set; }

        public IChannel Channel { get; set; }

        public DateTime LastActiveTime { get; set; }

        public DateTime StartTime { get; set; }
    }
}
