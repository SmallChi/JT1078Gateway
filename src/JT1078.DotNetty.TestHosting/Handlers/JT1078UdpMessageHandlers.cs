using JT1078.DotNetty.Core.Interfaces;
using JT1078.Protocol;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Threading.Tasks;

namespace JT1078.DotNetty.TestHosting.Handlers
{
    public class JT1078UdpMessageHandlers : IJT1078UdpMessageHandlers
    {
        private readonly ILogger<JT1078UdpMessageHandlers> logger;

        public JT1078UdpMessageHandlers(ILoggerFactory loggerFactory)
        {
            logger = loggerFactory.CreateLogger<JT1078UdpMessageHandlers>();
        }

        public Task Processor(JT1078Package package)
        {
            logger.LogDebug(JsonConvert.SerializeObject(package));
            return Task.CompletedTask;
        }
    }
}
