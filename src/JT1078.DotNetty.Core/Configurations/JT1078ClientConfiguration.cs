using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace JT1078.DotNetty.Core.Configurations
{
    public class JT1078ClientConfiguration
    {
        public string Host { get; set; }

        public int Port { get; set; }

        private EndPoint endPoint;

        public EndPoint EndPoint
        {
            get
            {
                if (endPoint == null)
                {
                    if (IPAddress.TryParse(Host, out IPAddress ip))
                    {
                        endPoint = new IPEndPoint(ip, Port);
                    }
                }
                return endPoint;
            }
        }
    }
}
