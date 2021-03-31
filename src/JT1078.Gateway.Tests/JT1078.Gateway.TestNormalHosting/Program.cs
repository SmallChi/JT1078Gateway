using JT1078.Flv;
using JT1078.Gateway.InMemoryMQ;
using JT1078.Gateway.TestNormalHosting.Services;
using JT1078.Hls;
using JT1078.Hls.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NLog.Extensions.Logging;
using System;
using System.IO;
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
                    logging.SetMinimumLevel(LogLevel.Trace);
                    Console.WriteLine($"Environment.OSVersion.Platform:{Environment.OSVersion.Platform.ToString()}");
                    NLog.LogManager.LoadConfiguration($"Configs/nlog.{Environment.OSVersion.Platform.ToString()}.config");
                    logging.AddNLog(new NLogProviderOptions { CaptureMessageTemplates = true, CaptureMessageProperties = true });
                })
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddMemoryCache();                  
                    services.AddSingleton<ILoggerFactory, LoggerFactory>();
                    services.AddSingleton(typeof(ILogger<>), typeof(Logger<>));
                    //flv视频解码器
                    services.AddSingleton<FlvEncoder>();
                    //hls视频解码器
                    services.AddSingleton<TSEncoder>();
                    services.AddSingleton<M3U8FileManage>();
                    //添加hls依赖项
                    services.AddHlsGateway(hostContext.Configuration);
                    services.Configure<M3U8Option>(hostContext.Configuration.GetSection("M3U8Option"));
                    var m3u8Option = services.BuildServiceProvider().GetRequiredService<IOptions<M3U8Option>>().Value;
                    services.AddSingleton(m3u8Option);

                    
                    //使用内存队列实现会话通知
                    services.AddJT1078Gateway(hostContext.Configuration)
                            .AddTcp()
                            .AddUdp()
                            .AddHttp()
                            //.AddCoordinatorHttpClient()
                            .AddMsgProducer()
                            .AddMsgConsumer();
                    //内存队列没有做分发，可以自己实现。
                    services.AddHostedService<JT1078FlvNormalMsgHostedService>();
                    services.AddHostedService<JT1078HlsNormalMsgHostedService>();

                    services.AddSingleton<MessageDispatchDataService>();
                    services.AddHostedService<MessageDispatchHostedService>();
                    
                });
            //测试1:
            //发送完整包
            //303163648162000000190130503701010000016f95973f840000000003b600000001674d001495a85825900000000168ee3c800000000106e5010d800000000165b80000090350bfdfe840b5b35c676079446e3ffe2f5240e25cc6b35cebac31720656a853ba1b571246fb858eaa5d8266acb95b92705fd187f1fd20ff0ca6b62c4cbcb3b662f5d61c016928ca82b411acdc4df6edb2034624b992eee9b6c241e1903bf9477c6e4293b65ba75e98d5a2566da6f71c85e1052a9d5ed35c393b1a73b181598749f3d26f6fbf48f0be61c673fcb9f2b0d305794bed03af5e3cedff7768bed3120261d6f3547a6d519943c2afcb80e423c9e6db088a06200dbfaa81edc5bc0de67957e791f67bf040ef944f7d62983d32517b2fb2d9572a71340c225617231bc0d98e66d19fe81a19b44280860b273f700bf3f3444a928e93fefc716e2af46995fbb658d0580a49e42f6835270c8c154abe28a17f76550b1b1fafe62945f80490b3f780fe9bb4d4b4107eac3d50b8c99d1a191f6754992096683fb0f599846bae759b06222079f5404be39e4416136c7c42255b0e7ca42d86fc2227892406d61f9816bc125d017989a671f13f2f4052e018b1fb02460802029a049a23d2ffeea6ac552109d35aa8731483fb2cae963987156056cafb32436a23a0dc918fb2440b14c9e6124441e7bb3b08706066d1ddab512267767b6e522f80732e67046ff5ad4d8193bf5cc5c05ccceb73a36b6c3ea39fa91bb308c8bb7bf88515d9c52409128e8b94e33e48a5396c35c20bd83b7c0e6d3d4a24bc14e84810066c686c6c04e687c41123fe87c89d5fa07b0095e7f82d3b07e72570163c47444bdde16ae9bfacd540df047e8ee34e98ff33178da5c7e6be9272e6dcfbb6db7e678a6d1d3832226c9bf85afa14feac15a270d5d3724a121b8fc9b40f0d37bb7f432de5421d286a65313a6efd251f7ed75b4ef6557975af5da5df2b87a0bbc1cb58183c4c1e24fdc4eb016777af1a6fa4a29d3eed7c4463482e591a6dc20540cabb6d7dd29cbb8ffdacafdaac2dd36db70fefe14fdeec85ef5fe01bb104d2d6439dbd7ceefc87007ce07b8409751dd7c21aa9a537f5fdefdef7d6ceba8d5ae876522f75dedd472e4dde1284e71380ee75ed313b2b9b9a94a56ebd03ae36b64a3b35abbdc7ba380016218201d156658ed9b5632f80f921879063e9037cd3509d01a2e91c17e03d892e2bc381ac723eba266497a1fbb0dc77ab3f4a9a981f95977b025b005a0e09b1add481888333927963fc5e5bf376655cb00e4ca8841fa450c8653f91cf2f3fb0247dbcace5dfde3af4a854f9fa2aaaa33706a78321332273ab4ee837ff4f8eba08676e7f889464a842b8e3e4a579d2
            //测试2：
            //发送头部包
            //303163648162000000190130503701010000016f95973f840000000003b6
            //发送数据体
            //00000001674d001495a85825900000000168ee3c800000000106e5010d800000000165b80000090350bfdfe840b5b35c676079446e3ffe2f5240e25cc6b35cebac31720656a853ba1b571246fb858eaa5d8266acb95b92705fd187f1fd20ff0ca6b62c4cbcb3b662f5d61c016928ca82b411acdc4df6edb2034624b992eee9b6c241e1903bf9477c6e4293b65ba75e98d5a2566da6f71c85e1052a9d5ed35c393b1a73b181598749f3d26f6fbf48f0be61c673fcb9f2b0d305794bed03af5e3cedff7768bed3120261d6f3547a6d519943c2afcb80e423c9e6db088a06200dbfaa81edc5bc0de67957e791f67bf040ef944f7d62983d32517b2fb2d9572a71340c225617231bc0d98e66d19fe81a19b44280860b273f700bf3f3444a928e93fefc716e2af46995fbb658d0580a49e42f6835270c8c154abe28a17f76550b1b1fafe62945f80490b3f780fe9bb4d4b4107eac3d50b8c99d1a191f6754992096683fb0f599846bae759b06222079f5404be39e4416136c7c42255b0e7ca42d86fc2227892406d61f9816bc125d017989a671f13f2f4052e018b1fb02460802029a049a23d2ffeea6ac552109d35aa8731483fb2cae963987156056cafb32436a23a0dc918fb2440b14c9e6124441e7bb3b08706066d1ddab512267767b6e522f80732e67046ff5ad4d8193bf5cc5c05ccceb73a36b6c3ea39fa91bb308c8bb7bf88515d9c52409128e8b94e33e48a5396c35c20bd83b7c0e6d3d4a24bc14e84810066c686c6c04e687c41123fe87c89d5fa07b0095e7f82d3b07e72570163c47444bdde16ae9bfacd540df047e8ee34e98ff33178da5c7e6be9272e6dcfbb6db7e678a6d1d3832226c9bf85afa14feac15a270d5d3724a121b8fc9b40f0d37bb7f432de5421d286a65313a6efd251f7ed75b4ef6557975af5da5df2b87a0bbc1cb58183c4c1e24fdc4eb016777af1a6fa4a29d3eed7c4463482e591a6dc20540cabb6d7dd29cbb8ffdacafdaac2dd36db70fefe14fdeec85ef5fe01bb104d2d6439dbd7ceefc87007ce07b8409751dd7c21aa9a537f5fdefdef7d6ceba8d5ae876522f75dedd472e4dde1284e71380ee75ed313b2b9b9a94a56ebd03ae36b64a3b35abbdc7ba380016218201d156658ed9b5632f80f921879063e9037cd3509d01a2e91c17e03d892e2bc381ac723eba266497a1fbb0dc77ab3f4a9a981f95977b025b005a0e09b1add481888333927963fc5e5bf376655cb00e4ca8841fa450c8653f91cf2f3fb0247dbcace5dfde3af4a854f9fa2aaaa33706a78321332273ab4ee837ff4f8eba08676e7f889464a842b8e3e4a579d2
            await serverHostBuilder.RunConsoleAsync();
        }
    }
}
