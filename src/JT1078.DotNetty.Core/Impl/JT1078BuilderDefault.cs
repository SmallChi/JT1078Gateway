using System;
using System.Collections.Generic;
using System.Text;
using JT1078.DotNetty.Core.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace JT1078.DotNetty.Core.Impl
{
    sealed class JT1078BuilderDefault : IJT1078Builder
    {
        public IServiceCollection Services { get; }

        public JT1078BuilderDefault(IServiceCollection services)
        {
            Services = services;
        }

        public IJT1078Builder Replace<T>() where T : IJT1078SourcePackageDispatcher
        {
            Services.Replace(new ServiceDescriptor(typeof(IJT1078SourcePackageDispatcher), typeof(T), ServiceLifetime.Singleton));
            return this;
        }
    }
}
