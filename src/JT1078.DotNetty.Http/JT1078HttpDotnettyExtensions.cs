using JT1078.DotNetty.Core.Codecs;
using JT1078.DotNetty.Core.Impl;
using JT1078.DotNetty.Core.Interfaces;
using JT1078.DotNetty.Core.Session;
using JT1078.DotNetty.Http.Authorization;
using JT1078.DotNetty.Http.Handlers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System.Runtime.CompilerServices;


namespace JT1078.DotNetty.Http
{
    public static class JT1078HttpDotnettyExtensions
    {
        public static IJT1078HttpBuilder AddJT1078HttpHost(this IJT1078Builder builder)
        {
            builder.Services.TryAddSingleton<JT1078HttpSessionManager>();
            builder.Services.TryAddSingleton<IJT1078Authorization, JT1078AuthorizationDefault>();
            builder.Services.AddScoped<JT1078HttpServerHandler>();
            builder.Services.AddHostedService<JT1078HttpServerHost>();
            return new JT1078HttpBuilderDefault(builder);
        }
    }
}