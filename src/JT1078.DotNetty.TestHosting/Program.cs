using JT1078.DotNetty.Core;
using JT1078.DotNetty.Tcp;
using JT1078.DotNetty.TestHosting.Handlers;
using JT1078.DotNetty.Udp;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace JT1078.DotNetty.TestHosting
{
    class Program
    {
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
                    logging.SetMinimumLevel(LogLevel.Trace);
                    logging.AddConsole();
                })
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddSingleton<ILoggerFactory, LoggerFactory>();
                    services.AddSingleton(typeof(ILogger<>), typeof(Logger<>));
                    services.AddJT1078Core(hostContext.Configuration)
                            .AddJT1078TcpHost()
                            .Replace<JT1078TcpMessageHandlers>()
                            .Builder()
                            .AddJT1078UdpHost()
                            .Replace<JT1078UdpMessageHandlers>()
                            .Builder();
                });

            await serverHostBuilder.RunConsoleAsync();
        }
    }
}
