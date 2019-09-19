using JT1078.DotNetty.Core.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System;
using System.Collections.Generic;
using System.Text;

namespace JT1078.DotNetty.Http
{
    class JT1078HttpBuilderDefault : IJT1078HttpBuilder
    {
        public IJT1078Builder Instance { get; }

        public JT1078HttpBuilderDefault(IJT1078Builder builder)
        {
            Instance = builder;
        }

        public IJT1078Builder Builder()
        {
            return Instance;
        }

        public IJT1078HttpBuilder Replace<T>() where T : IJT1078Authorization
        {
            Instance.Services.Replace(new ServiceDescriptor(typeof(IJT1078Authorization), typeof(T), ServiceLifetime.Singleton));
            return this;
        }

        public IJT1078HttpBuilder UseHttpMiddleware<T>() where T : IHttpMiddleware
        {
            Instance.Services.TryAdd(new ServiceDescriptor(typeof(IHttpMiddleware), typeof(T), ServiceLifetime.Singleton));
            return this;
        }
    }
}
