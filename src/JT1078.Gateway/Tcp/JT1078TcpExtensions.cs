using JT1078.Gateway.Codecs;
using JT1078.Gateway.Interfaces;
using JT1078.Gateway.Session;
using JT1078.Gateway.Tcp;
using JT1078.Gateway.Tcp.Handlers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System.Runtime.CompilerServices;


namespace JT1078.Gateway
{
    public static class JT1078TcpExtensions
    {
        public static IJT1078TcpBuilder AddTcpHost(this IJT1078Builder builder)
        {
            builder.Services.TryAddSingleton<JT1078TcpSessionManager>();
            builder.Services.TryAddScoped<JT1078TcpConnectionHandler>();
            builder.Services.TryAddScoped<JT1078TcpDecoder>();
            builder.Services.TryAddSingleton<IJT1078TcpMessageHandlers, JT1078TcpMessageProcessorEmptyImpl>();
            builder.Services.TryAddScoped<JT1078TcpServerHandler>();
            builder.Services.AddHostedService<JT1078TcpServerHost>();
            return new JT1078TcpBuilderDefault(builder);
        }
    }
}