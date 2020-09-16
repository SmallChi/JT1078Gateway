using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace JT1078.Gateway.Abstractions
{
    public interface IJT1078MsgConsumer : IJT1078PubSub, IDisposable
    {
        void OnMessage(Action<(string SIM, byte[] Data)> callback);
        CancellationTokenSource Cts { get; }
        void Subscribe();
        void Unsubscribe();
    }
}
