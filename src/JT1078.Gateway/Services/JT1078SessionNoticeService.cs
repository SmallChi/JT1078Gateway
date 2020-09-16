using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;

namespace JT1078.Gateway.Services
{
    public class JT1078SessionNoticeService
    {
        public BlockingCollection<(string SessionType, string SIM,string ProtocolType)> SessionNoticeBlockingCollection { get;internal set; }
        public JT1078SessionNoticeService()
        {
            SessionNoticeBlockingCollection = new BlockingCollection<(string SessionType, string SIM, string ProtocolType)>();
        }
    }
}
