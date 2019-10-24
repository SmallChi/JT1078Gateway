using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;

namespace JT1078.Gateway.Interfaces
{
    public interface IJT1078TcpBuilder
    {
        IJT1078Builder Instance { get;}
        IJT1078Builder Builder();
        IJT1078TcpBuilder Replace<T>() where T : IJT1078TcpMessageHandlers;
    }
}
