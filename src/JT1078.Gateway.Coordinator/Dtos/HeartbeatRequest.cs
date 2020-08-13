using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace JT1078.Gateway.Coordinator.Dtos
{
    public class HeartbeatRequest
    {
        public int HttpPort { get; set; }
        public int TcpPort { get; set; }
        public int UdpPort { get; set; }
        public int TcpSessionCount { get; set; }
        public int UdpSessionCount { get; set; }
        public int HttpSessionCount { get; set; }
        public int WebSocketSessionCount { get; set; }
    }
}
