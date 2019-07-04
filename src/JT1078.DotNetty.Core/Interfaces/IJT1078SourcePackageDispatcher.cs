using System.Threading.Tasks;

namespace JT1078.DotNetty.Core.Interfaces
{
    /// <summary>
    /// 源包分发器
    /// </summary>
    public interface IJT1078SourcePackageDispatcher
    {
        Task SendAsync(byte[] data);
    }
}
