using JT1078.Gateway.Http;
using JT1078.Gateway.Http.Authorization;
using JT1078.Gateway.Http.Handlers;
using JT1078.Gateway.Interfaces;
using JT1078.Gateway.Session;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System.Runtime.CompilerServices;


namespace JT1078.Gateway
{
    public static class JT1078HttpDotnettyExtensions
    {
        public static IJT1078HttpBuilder AddHttpHost(this IJT1078Builder builder)
        {
            builder.Services.TryAddSingleton<JT1078HttpSessionManager>();
            builder.Services.TryAddSingleton<IJT1078Authorization, JT1078AuthorizationDefault>();
            builder.Services.AddScoped<JT1078HttpServerHandler>();
            builder.Services.AddHostedService<JT1078HttpServerHost>();
            return new JT1078HttpBuilderDefault(builder);
        }
    }
}