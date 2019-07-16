using JT1078.Protocol;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;

namespace JT1078.DotNetty.TestHosting
{
    public class JT1078DataService
    {
        public BlockingCollection<JT1078Package> DataBlockingCollection { get; }

        public JT1078DataService()
        {
            DataBlockingCollection = new BlockingCollection<JT1078Package>();
        }
    }
}
