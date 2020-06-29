using JT1078.Gateway.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace JT1078.Gateway.Impl
{
    sealed class JT1078BuilderDefault : IJT1078Builder
    {
        public IServiceCollection Services { get; }

        public JT1078BuilderDefault(IServiceCollection services)
        {
            Services = services;
        }
    }
}
