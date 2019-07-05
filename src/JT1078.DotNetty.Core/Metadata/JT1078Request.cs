using JT1078.Protocol;
using System;
using System.Collections.Generic;
using System.Text;

namespace JT1078.DotNetty.Core.Metadata
{
    public class JT1078Request
    {
        public JT1078Request(JT1078Package package,byte[] src)
        {
            Package = package;
            Src = src;
        }

        public JT1078Package Package { get; }

        public byte[] Src { get; }
    }
}
