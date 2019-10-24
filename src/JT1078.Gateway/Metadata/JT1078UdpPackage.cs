using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace JT1078.Gateway.Metadata
{
    public  class JT1078UdpPackage
    {
        public JT1078UdpPackage(byte[] buffer, EndPoint sender)
        {
            Buffer = buffer;
            Sender = sender;
        }

        public byte[] Buffer { get; }

        public EndPoint Sender { get; }
    }
}
