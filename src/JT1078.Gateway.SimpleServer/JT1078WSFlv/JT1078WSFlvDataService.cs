using System;
using System.Collections.Generic;
using System.Text;
using System.Collections.Concurrent;
using JT1078.Protocol;

namespace JT1078.Gateway.SimpleServer.JT1078WSFlv
{
    public class JT1078WSFlvDataService
    {
        public JT1078WSFlvDataService()
        {
            JT1078Packages = new BlockingCollection<JT1078Package>();
        }
        public BlockingCollection<JT1078Package> JT1078Packages { get; set; }
    }
}
