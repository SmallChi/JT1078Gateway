using JT1078.Gateway.Codecs;
using JT1078.Gateway.Interfaces;
using JT1078.Gateway.Session;
using JT1078.Gateway.Udp;
using JT1078.Gateway.Udp.Handlers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System.Runtime.CompilerServices;

namespace JT1078.Gateway
{
    public static class JT1078UdpExtensions
    {
        public static IJT1078UdpBuilder AddUdpHost(this IJT1078Builder builder)
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