using JT1078.Protocol;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace JT1078.Gateway.Abstractions
{
    public interface IJT1078PackageConsumer : IJT1078PubSub, IDisposable
    {
        void OnMessage(Action<(string TerminalNo, JT1078Package Data)> callback);
        CancellationTokenSource Cts { get; }
        void Subscribe();
        void Unsubscribe();
    }
}
