using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;

namespace JT1078.Gateway.Interfaces
{
    public interface IJT1078HttpBuilder
    {
        IJT1078Builder Instance { get; }
        IJT1078Builder Builder();
        IJT1078HttpBuilder Replace<T>() where T : IJT1078Authorization;
        IJT1078HttpBuilder UseHttpMiddleware<T>() where T : IHttpMiddleware;
    }
}
