using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace JT1078.Gateway.Abstractions
{
    public interface IJT1078MsgProducer : IJT1078PubSub, IDisposable
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="sim">设备sim终端号</param>
        /// <param name="data">jt1078 hex data</param>
        /// <param name="cancellationToken">cts</param>
        ValueTask ProduceAsync(string sim, byte[] data, CancellationToken cancellationToken = default);
    }
}
