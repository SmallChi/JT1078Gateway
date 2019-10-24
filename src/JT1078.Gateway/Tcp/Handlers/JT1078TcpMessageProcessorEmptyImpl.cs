using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using JT1078.Gateway.Interfaces;
using JT1078.Gateway.Metadata;
using JT1078.Protocol;

namespace JT1078.Gateway.Tcp.Handlers
{
    class JT1078TcpMessageProcessorEmptyImpl : IJT1078TcpMessageHandlers
    {
        public Task<JT1078Response> Processor(JT1078Request request)
        {
            return Task.FromResult<JT1078Response>(default);
        }
    }
}
