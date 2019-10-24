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
       
        public int QuietPeriodSeconds { get; set; } = 1;

        public TimeSpan QuietPeriodTimeSpan => TimeSpan.FromSeconds(QuietPeriodSeconds);

        public int ShutdownTimeoutSeconds { get; set; } = 3;

        public TimeSpan ShutdownTimeoutTimeSpan => TimeSpan.FromSeconds(ShutdownTimeoutSeconds);

        public int SoBacklog { get; set; } = 8192;

        public int EventLoopCount { get; set; } = Environment.ProcessorCount;

        public int ReaderIdleTimeSeconds { get; set; } = 3600;

        public int WriterIdleTimeSeconds { get; set; } = 3600;

        public int AllIdleTimeSeconds { get; set; } = 3600;

        public JT1078RemoteServerOptions RemoteServerOptions { get; set; }

        public JT1078Configuration Value => this;
    }
}
