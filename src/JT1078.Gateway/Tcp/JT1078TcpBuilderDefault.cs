using JT1078.Gateway.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System;
using System.Collections.Generic;
using System.Text;

namespace JT1078.Gateway.Tcp
{
    class JT1078TcpBuilderDefault : IJT1078TcpBuilder
    {
        public IJT1078Builder Instance { get; }

        public JT1078TcpBuilderDefault(IJT1078Builder builder)
        {
            Instance = builder;
        }

        public IJT1078Builder Builder()
        {
            return Instance;
        }

        public IJT1078TcpBuilder Replace<T>() where T : IJT1078TcpMessageHandlers
        {
            Instance.Services.Replace(new ServiceDescriptor(typeof(IJT1078TcpMessageHandlers), typeof(T), ServiceLifetime.Singleton));
            return this;
        }
    }
}
