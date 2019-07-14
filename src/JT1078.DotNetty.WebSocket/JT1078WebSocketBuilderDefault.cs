using JT1078.DotNetty.Core.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System;
using System.Collections.Generic;
using System.Text;

namespace JT1078.DotNetty.WebSocket
{
    class JT1078WebSocketBuilderDefault : IJT1078WebSocketBuilder
    {
        public IJT1078Builder Instance { get; }

        public JT1078WebSocketBuilderDefault(IJT1078Builder builder)
        {
            Instance = builder;
        }

        public IJT1078Builder Builder()
        {
            return Instance;
        }
    }
}
