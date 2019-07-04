using JT1078.DotNetty.Core.Codecs;
using JT1078.DotNetty.Core.Interfaces;
using JT1078.DotNetty.Core.Session;
using JT1078.DotNetty.Tcp.Handlers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("JT1078.DotNetty.Tcp.Test")]

namespace JT1078.DotNetty.Tcp
{
    public static class JT1078TcpDotnettyExtensions
    {
        public static IJT1078TcpBuilder AddJT1078TcpHost(this IJT1078Builder builder)
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