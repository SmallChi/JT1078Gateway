using JT1078.DotNetty.Core.Metadata;

namespace JT1078.DotNetty.Core.Services
{
    /// <summary>
    /// 计数包服务
    /// </summary>
    public class JT1078AtomicCounterService
    {
        private readonly JT1078AtomicCounter MsgSuccessCounter;

        private readonly JT1078AtomicCounter MsgFailCounter;

        public JT1078AtomicCounterService()
        {
            MsgSuccessCounter=new JT1078AtomicCounter();
            MsgFailCounter = new JT1078AtomicCounter();
        }

        public void Reset()
        {
            MsgSuccessCounter.Reset();
            MsgFailCounter.Reset();
        }

        public long MsgSuccessIncrement()
        {
            return MsgSuccessCounter.Increment();
        }

        public long MsgSuccessCount
        {
            get
            {
                return MsgSuccessCounter.Count;
            }
        }

        public long MsgFailIncrement()
        {
            return MsgFailCounter.Increment();
        }

        public long MsgFailCount
        {
            get
            {
                return MsgFailCounter.Count;
            }
        }
    }
}
