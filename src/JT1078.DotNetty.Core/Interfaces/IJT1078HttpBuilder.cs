using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;

namespace JT1078.DotNetty.Core.Interfaces
{
    public interface IJT1078HttpBuilder
    {
        IJT1078Builder Instance { get; }
        IJT1078Builder Builder();
    }
}
