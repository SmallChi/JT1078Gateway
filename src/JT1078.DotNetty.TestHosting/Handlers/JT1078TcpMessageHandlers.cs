using DotNetty.Buffers;
using JT1078.DotNetty.Core.Interfaces;
using JT1078.DotNetty.Core.Metadata;
using JT1078.Protocol;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace JT1078.DotNetty.TestHosting.Handlers
{
    public class JT1078TcpMessageHandlers : IJT1078TcpMessageHandlers
    {
        private readonly ILogger logger;
        private readonly ILogger hexLogger;
        public JT1078TcpMessageHandlers(
            ILoggerFactory loggerFactory)
        {
            logger = loggerFactory.CreateLogger("JT1078TcpMessageHandlers");
            hexLogger = loggerFactory.CreateLogger("JT1078TcpMessageHandlersHex");
        }

        public Task<JT1078Response> Processor(JT1078Request request)
        {
            logger.LogInformation(JsonConvert.SerializeObject(request.Package));
            hexLogger.LogInformation($"{request.Package.SIM},{request.Package.SN},{request.Package.LogicChannelNumber},{request.Package.Label3.DataType.ToString()},{request.Package.Label3.SubpackageType.ToString()},{ByteBufferUtil.HexDump(request.Src)}");
            return Task.FromResult<JT1078Response>(default);
        }
    }
}
