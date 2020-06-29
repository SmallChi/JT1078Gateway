using JT1078.Protocol;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace JT1078.Gateway.Abstractions
{
    public interface IJT1078PackageProducer : IJT1078PubSub, IDisposable
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="terminalNo">设备终端号</param>
        /// <param name="data">jt1078 package data</param>
        ValueTask ProduceAsync(string terminalNo, JT1078Package data);
    }
}
