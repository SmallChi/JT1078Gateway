using JT1078.DotNetty.Core.Codecs;
using JT1078.DotNetty.Core.Interfaces;
using JT1078.DotNetty.Core.Session;
using JT1078.DotNetty.Udp;
using JT1078.DotNetty.Udp.Handlers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("JT1078.DotNetty.Udp.Test")]

namespace JT1078.DotNetty.Udp
{
    public static class JT1078UdpDotnettyExtensions
    {
        public static IJT1078UdpBuilder AddJT1078UdpHost(this IJT1078Builder builder)
        {
            builder.Services.TryAddSingleton<JT1078UdpSessionManager>();
            builder.Services.TryAddSingleton<IJT1078UdpMessageHandlers, JT1078UdpMessageProcessorEmptyImpl>();
            builder.Services.TryAddScoped<JT1078UdpDecoder>();
            builder.Services.TryAddScoped<JT1078UdpServerHandler>();
            builder.Services.AddHostedService<JT1078UdpServerHost>();
            return new JT1078UdpBuilderDefault(builder);
        }
    }
}