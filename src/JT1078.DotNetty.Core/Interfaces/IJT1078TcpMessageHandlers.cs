using JT1078.DotNetty.Core.Metadata;
using JT1078.Protocol;
using System.Threading.Tasks;

namespace JT1078.DotNetty.Core.Interfaces
{
    public interface IJT1078TcpMessageHandlers
    {
        Task<JT1078Response> Processor(JT1078Request request);
    }
}
