using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Text;

namespace JT1078.Gateway.Configurations
{
    public class JT1078Configuration : IOptions<JT1078Configuration>
    {
        public int TcpPort { get; set; } = 1078;
        public int UdpPort { get; set; } = 1078;
        public int HttpPort { get; set; } = 1079;
        public int SoBacklog { get; set; } = 8192;
        public int MiniNumBufferSize { get; set; } = 8096;
        /// <summary>
        /// Tcp读超时 
        /// 默认10分钟检查一次
        /// </summary>
        public int TcpReaderIdleTimeSeconds { get; set; } = 60 * 10;
        /// <summary>
        /// Tcp 60s检查一次
        /// </summary>
        public int TcpReceiveTimeoutCheckTimeSeconds { get; set; } = 60;
        /// <summary>
        /// Udp读超时
        /// </summary>
        public int UdpReaderIdleTimeSeconds { get; set; } = 60;
        /// <summary>
        /// Udp 60s检查一次
        /// </summary>
        public int UdpReceiveTimeoutCheckTimeSeconds { get; set; } = 60;

        public JT1078Configuration Value => this;
    }
}
