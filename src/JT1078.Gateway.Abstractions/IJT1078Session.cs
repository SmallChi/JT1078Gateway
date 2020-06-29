using JT1078.Gateway.Abstractions.Enums;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace JT1078.Gateway.Abstractions
{
    public interface IJT1078Session
    {
        /// <summary>
        /// 终端手机号
        /// </summary>
        string TerminalPhoneNo { get; set; }
        string SessionID { get; }
        Socket Client { get; set; }
        DateTime StartTime { get; set; }
        DateTime ActiveTime { get; set; }
        JT1078TransportProtocolType TransportProtocolType { get;}
        CancellationTokenSource ReceiveTimeout { get; set; }
        EndPoint RemoteEndPoint { get; set; }
        void Close();
    }
}
