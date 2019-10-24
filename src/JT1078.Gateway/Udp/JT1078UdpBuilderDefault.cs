using JT1078.Gateway.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System;
using System.Collections.Generic;
using System.Text;

namespace JT1078.Gateway.Udp
{
    class JT1078UdpBuilderDefault : IJT1078UdpBuilder
    {
        public IJT1078Builder Instance { get; }

        public JT1078UdpBuilderDefault(IJT1078Builder builder)
        {
            Instance = builder;
        }

        public IJT1078Builder Builder()
        {
            return Instance;
        }

        public IJT1078UdpBuilder Replace<T>() where T : IJT1078UdpMessageHandlers
        {
            Instance.Services.Replace(new ServiceDescriptor(typeof(IJT1078UdpMessageHandlers), typeof(T), ServiceLifetime.Singleton));
            return this;
        }
    }
}
