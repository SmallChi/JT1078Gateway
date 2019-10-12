using JT1078.DotNetty.Core;
using JT1078.DotNetty.Tcp;
using JT1078.DotNetty.TestHosting.Handlers;
using JT1078.DotNetty.Udp;
using JT1078.DotNetty.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using NLog.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using JT1078.DotNetty.TestHosting.JT1078WSFlv;

namespace JT1078.DotNetty.TestHosting
{
    class Program
    {
        static Program()
        {
            Newtonsoft.Json.JsonSerializerSettings setting = new Newtonsoft.Json.JsonSerializerSettings();
            JsonConvert.DefaultSettings = new Func<JsonSerializerSettings>(() =>
            {
                setting.Converters.Add(new StringEnumConverter());
                return setting;
            });
        }
        static async Task Main(string[] args)
        {
            //3031636481E2108801123456781001100000016BB392CA7C02800028002E0000000161E1A2BF0098CFC0EE1E17283407788E39A403FDDBD1D546BFB063013F59AC34C97A021AB96A28A42C08
            var serverHostBuilder = new HostBuilder()
                .ConfigureAppConfiguration((hostingContext, config) =>
                {
                    config.SetBasePath(AppDomain.CurrentDomain.BaseDirectory);
                    config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
                })
                .ConfigureLogging((context, logging) =>
                {
                    if (Environment.OSVersion.Platform == PlatformID.Unix)
                    {
                        NLog.LogManager.LoadConfiguration("Configs/nlog.unix.config");
                    }
                    else
                    {
                        NLog.LogManager.LoadConfiguration("Configs/nlog.win.config");
                    }
                    logging.AddNLog(new NLogProviderOptions { CaptureMessageTemplates = true, CaptureMessageProperties = true });
                    logging.SetMinimumLevel(LogLevel.Trace);
                })
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddSingleton<JT1078WSFlvDataService>();
                    services.AddSingleton<ILoggerFactory, LoggerFactory>();
                    services.AddSingleton(typeof(ILogger<>), typeof(Logger<>));
                    services.AddJT1078Core(hostContext.Configuration)
                            .AddJT1078TcpHost()
                            .Replace<JT1078TcpMessageHandlers>()
                            .Builder()
                            //.AddJT1078UdpHost()
                            //.Replace<JT1078UdpMessageHandlers>()
                            // .Builder()
                           .AddJT1078HttpHost()
                           //.UseHttpMiddleware<CustomHttpMiddleware>()
                           //.Builder()
                           ;
                    //使用ffmpeg工具
                    //1.success
                    //services.AddHostedService<FFMPEGRTMPHostedService>();
                    //2.success
                    //services.AddHostedService<FFMPEGHTTPFLVHostedService>();
                    //3.success
                    //services.AddHostedService<FFMPEGWSFLVPHostedService>();
                    //4.success
                    //http://127.0.0.1:5001/HLS/hls.html
                    //services.AddHostedService<FFMPEGHLSHostedService>();

                    services.AddHostedService<JT1078WSFlvHostedService>();
                });
            await serverHostBuilder.RunConsoleAsync();
        }
    }
}
