using JT1078.DotNetty.Core.Configurations;
using JT1078.DotNetty.Core.Converters;
using JT1078.DotNetty.Core.Impl;
using JT1078.DotNetty.Core.Interfaces;
using JT1078.DotNetty.Core.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("JT1078.DotNetty.Core.Test")]
[assembly: InternalsVisibleTo("JT1078.DotNetty.Tcp.Test")]
[assembly: InternalsVisibleTo("JT1078.DotNetty.Udp.Test")]
[assembly: InternalsVisibleTo("JT1078.DotNetty.Tcp")]
[assembly: InternalsVisibleTo("JT1078.DotNetty.Udp")]
namespace JT1078.DotNetty.Core
{
    public static class JT1078CoreDotnettyExtensions
    {
        static JT1078CoreDotnettyExtensions()
        {
            JsonConvert.DefaultSettings = new Func<JsonSerializerSettings>(() =>
            {
                Newtonsoft.Json.JsonSerializerSettings settings = new Newtonsoft.Json.JsonSerializerSettings();
                //日期类型默认格式化处理
                settings.DateFormatHandling = Newtonsoft.Json.DateFormatHandling.MicrosoftDateFormat;
                settings.DateFormatString = "yyyy-MM-dd HH:mm:ss";
                //空值处理
                settings.NullValueHandling = NullValueHandling.Ignore;
                settings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
                settings.Converters.Add(new JsonIPAddressConverter());
                settings.Converters.Add(new JsonIPEndPointConverter());
                return settings;
            });
        }

        public static IJT1078Builder AddJT1078Core(this IServiceCollection  serviceDescriptors, IConfiguration configuration, Newtonsoft.Json.JsonSerializerSettings settings=null)
        {
            if (settings != null)
            {
                JsonConvert.DefaultSettings = new Func<JsonSerializerSettings>(() =>
                {
                    settings.Converters.Add(new JsonIPAddressConverter());
                    settings.Converters.Add(new JsonIPEndPointConverter());
                    settings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
                    return settings;
                });
            }
            IJT1078Builder builder = new JT1078BuilderDefault(serviceDescriptors);
            builder.Services.Configure<JT1078Configuration>(configuration.GetSection("JT1078Configuration"));
            builder.Services.TryAddSingleton<JT1078AtomicCounterServiceFactory>();
            return builder;
        }

        public static IJT1078Builder AddJT1078Core(this IServiceCollection serviceDescriptors, Action<JT1078Configuration> jt1078Options, Newtonsoft.Json.JsonSerializerSettings settings = null)
        {
            if (settings != null)
            {
                JsonConvert.DefaultSettings = new Func<JsonSerializerSettings>(() =>
                {
                    settings.Converters.Add(new JsonIPAddressConverter());
                    settings.Converters.Add(new JsonIPEndPointConverter());
                    settings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
                    return settings;
                });
            }
            IJT1078Builder builder = new JT1078BuilderDefault(serviceDescriptors);
            builder.Services.Configure(jt1078Options);
            builder.Services.TryAddSingleton<JT1078AtomicCounterService>();
            builder.Services.TryAddSingleton<JT1078AtomicCounterServiceFactory>();
            return builder;
        }
    }
}