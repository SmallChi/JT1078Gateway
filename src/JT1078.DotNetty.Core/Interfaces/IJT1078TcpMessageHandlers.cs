using JT1078.Protocol;
using System.Threading.Tasks;

namespace JT1078.DotNetty.Core.Interfaces
{
    public interface IJT1078TcpMessageHandlers
    {
        Task Processor(JT1078Package package);
    }
}
