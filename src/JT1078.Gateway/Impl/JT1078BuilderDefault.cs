using System;
using System.Collections.Generic;
using System.Text;
using JT1078.Gateway.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

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
