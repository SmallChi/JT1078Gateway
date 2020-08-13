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
        /// <summary>
        /// Hls根目录
        /// </summary>
        public string HlsRootDirectory { get; set; } = "wwwroot";
        /// <summary>
        /// 协调器发送心跳时间
        /// 默认60s发送一次
        /// </summary>
        public int CoordinatorHeartbeatTimeSeconds { get; set; } = 60;
        /// <summary>
        /// 协调器Coordinator主机
        /// http://localhost/
        /// http://127.0.0.1/
        /// </summary>
        public string CoordinatorUri { get; set; } = "http://localhost:1080/";       
        /// <summary>
        /// 协调器Coordinator主机登录账号
        /// </summary>
        public string CoordinatorUserName { get; set; } = "admin";
        /// <summary>
        /// 协调器Coordinator主机登录密码
        /// </summary>
        public string CoordinatorPassword { get; set; } = "123456";
        public JT1078Configuration Value => this;
    }
}
