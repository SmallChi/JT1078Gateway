using JT1078.DotNetty.Core.Interfaces;
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
        private readonly ILogger<JT1078TcpMessageHandlers> logger;

        public JT1078TcpMessageHandlers(ILoggerFactory loggerFactory)
        {
            logger = loggerFactory.CreateLogger<JT1078TcpMessageHandlers>();
        }

        public Task Processor(JT1078Package package)
        {
            logger.LogDebug(JsonConvert.SerializeObject(package));
            return Task.CompletedTask;
        }
    }
}
