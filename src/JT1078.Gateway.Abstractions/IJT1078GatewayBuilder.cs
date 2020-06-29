using JT1078.Protocol;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;

namespace JT1078.Gateway.Abstractions
{
    public interface IJT1078GatewayBuilder
    {
        IJT1078Builder JT1078Builder { get; }
        IJT1078Builder Builder();
    }
}
