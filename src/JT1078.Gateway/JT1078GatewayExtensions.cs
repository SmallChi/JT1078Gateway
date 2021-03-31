﻿using JT1078.Gateway.Abstractions;
using JT1078.Gateway.Configurations;
using JT1078.Gateway.Impl;
using JT1078.Gateway.Jobs;
using JT1078.Gateway.Services;
using JT1078.Gateway.Sessions;
using JT1078.Hls.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using System;
using System.IO;
using System.Net.Http;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("JT1078.Gateway.Test")]

namespace JT1078.Gateway
{
    public static class JT1078GatewayExtensions
    {

        public static IJT1078GatewayBuilder AddJT1078Gateway(this IServiceCollection  serviceDescriptors, IConfiguration configuration)
        {
            IJT1078Builder builder = new JT1078BuilderDefault(serviceDescriptors);
            builder.Services.Configure<JT1078Configuration>(configuration.GetSection("JT1078Configuration"));
            IJT1078GatewayBuilder jT1078GatewayBuilderDefault = new JT1078GatewayBuilderDefault(builder);
            jT1078GatewayBuilderDefault.AddJT1078Core();
            return jT1078GatewayBuilderDefault;
        }

        public static IJT1078GatewayBuilder AddJT1078Gateway(this IServiceCollection serviceDescriptors, Action<JT1078Configuration> jt1078Options)
        {
            IJT1078Builder builder = new JT1078BuilderDefault(serviceDescriptors);
            builder.Services.Configure(jt1078Options);
            IJT1078GatewayBuilder jT1078GatewayBuilderDefault = new JT1078GatewayBuilderDefault(builder);
            jT1078GatewayBuilderDefault.AddJT1078Core();
            return jT1078GatewayBuilderDefault;
        }

        public static IJT1078GatewayBuilder AddTcp(this IJT1078GatewayBuilder builder)
        {
            builder.JT1078Builder.Services.AddHostedService<JT1078TcpReceiveTimeoutJob>();
            builder.JT1078Builder.Services.AddHostedService<JT1078TcpServer>();
            return builder;
        }

        public static IJT1078GatewayBuilder AddUdp(this IJT1078GatewayBuilder builder)
        {
            builder.JT1078Builder.Services.AddHostedService<JT1078UdpReceiveTimeoutJob>();
            builder.JT1078Builder.Services.AddHostedService<JT1078UdpServer>();
            return builder;
        }

        public static IJT1078GatewayBuilder AddHttp(this IJT1078GatewayBuilder builder)
        {
            builder.JT1078Builder.Services.AddSingleton<IJT1078Authorization, JT1078AuthorizationDefault>();
            builder.JT1078Builder.Services.AddSingleton<JT1078HttpSessionManager>();
            builder.JT1078Builder.Services.AddHostedService<JT1078HttpServer>();
            return builder;
        }

        public static IJT1078GatewayBuilder AddHttp<TIJT1078Authorization>(this IJT1078GatewayBuilder builder)
            where TIJT1078Authorization: IJT1078Authorization
        {
            builder.JT1078Builder.Services.AddSingleton(typeof(IJT1078Authorization), typeof(TIJT1078Authorization));
            builder.JT1078Builder.Services.AddSingleton<JT1078HttpSessionManager>();
            builder.JT1078Builder.Services.AddHostedService<JT1078HttpServer>();
            return builder;
        }
        
        public static IJT1078GatewayBuilder AddCoordinatorHttpClient(this IJT1078GatewayBuilder builder)
        {
            builder.JT1078Builder.Services.AddSingleton<JT1078CoordinatorHttpClient>();
            builder.JT1078Builder.Services.AddHostedService<JT1078HeartbeatJob>();
            return builder;
        }

        internal static IJT1078GatewayBuilder AddJT1078Core(this IJT1078GatewayBuilder builder)
        {
            builder.JT1078Builder.Services.AddSingleton<JT1078SessionNoticeService>();
            builder.JT1078Builder.Services.AddSingleton<JT1078SessionManager>();
            builder.JT1078Builder.Services.AddHostedService<JT1078SessionNoticeJob>();

            return builder;
        }
        public static IServiceCollection AddHlsGateway(this IServiceCollection serviceDescriptors, IConfiguration configuration)
        {
            serviceDescriptors.AddSingleton<HLSRequestManager>();
            serviceDescriptors.AddSingleton<HLSPathStorage>();
            serviceDescriptors.AddHostedService<JT1078SessionClearJob>();
            return serviceDescriptors;
        }
    }
}