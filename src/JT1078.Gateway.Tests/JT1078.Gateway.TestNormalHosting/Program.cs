using JT1078.Gateway.InMemoryMQ;
using JT1078.Gateway.TestNormalHosting.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace JT1078.Gateway.TestNormalHosting
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var serverHostBuilder = new HostBuilder()
                .ConfigureAppConfiguration((hostingContext, config) =>
                {
                    config.SetBasePath(AppDomain.CurrentDomain.BaseDirectory);
                    config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
                })
                .ConfigureLogging((context, logging) =>
                {
                    logging.AddConsole();
                    logging.SetMinimumLevel(LogLevel.Trace);
                })
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddSingleton<ILoggerFactory, LoggerFactory>();
                    services.AddSingleton(typeof(ILogger<>), typeof(Logger<>));
                    //使用内存队列实现会话通知
                    services.AddJT1078Gateway(hostContext.Configuration)
                            .AddTcp()
                            .AddNormal()
                            .AddMsgProducer()
                            .AddMsgConsumer();
                    services.AddHostedService<JT1078NormalMsgHostedService>();
                });

            await serverHostBuilder.RunConsoleAsync();
        }
    }
}
