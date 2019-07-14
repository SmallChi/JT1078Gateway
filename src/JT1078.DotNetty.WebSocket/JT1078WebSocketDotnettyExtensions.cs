using JT1078.DotNetty.Core.Codecs;
using JT1078.DotNetty.Core.Interfaces;
using JT1078.DotNetty.Core.Session;
using JT1078.DotNetty.WebSocket.Handlers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System.Runtime.CompilerServices;


namespace JT1078.DotNetty.WebSocket
{
    public static class JT1078WebSocketDotnettyExtensions
    {
        public static IJT1078WebSocketBuilder AddJT1078WebSocketHost(this IJT1078Builder builder)
        {
            builder.Services.TryAddSingleton<JT1078WebSocketSessionManager>();
            builder.Services.AddScoped<JT1078WebSocketServerHandler>();
            builder.Services.AddHostedService<JT1078WebSocketServerHost>();
            return new JT1078WebSocketBuilderDefault(builder);
        }
    }
}