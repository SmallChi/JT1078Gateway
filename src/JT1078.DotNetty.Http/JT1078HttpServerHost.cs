using DotNetty.Buffers;
using DotNetty.Codecs;
using DotNetty.Codecs.Http;
using DotNetty.Codecs.Http.Cors;
using DotNetty.Common.Utilities;
using DotNetty.Handlers.Streams;
using DotNetty.Handlers.Timeout;
using DotNetty.Transport.Bootstrapping;
using DotNetty.Transport.Channels;
using DotNetty.Transport.Libuv;
using JT1078.DotNetty.Core.Codecs;
using JT1078.DotNetty.Core.Configurations;
using JT1078.DotNetty.Http.Handlers;
using JT1078.Protocol;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Net;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace JT1078.DotNetty.Http
{
    /// <summary>
    /// JT1078 http服务
    /// </summary>
    internal class JT1078HttpServerHost : IHostedService
    {
        private readonly JT1078Configuration configuration;
        private readonly ILogger<JT1078HttpServerHost> logger;
        private DispatcherEventLoopGroup bossGroup;
        private WorkerEventLoopGroup workerGroup;
        private IChannel bootstrapChannel;
        private IByteBufferAllocator serverBufferAllocator;
        private readonly IServiceProvider serviceProvider;
        public JT1078HttpServerHost(
            IServiceProvider serviceProvider,
            ILoggerFactory loggerFactory,
            IOptions<JT1078Configuration> configurationAccessor)
        {
            this.serviceProvider = serviceProvider;
            configuration = configurationAccessor.Value;
            logger=loggerFactory.CreateLogger<JT1078HttpServerHost>();
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            bossGroup = new DispatcherEventLoopGroup();
            workerGroup = new WorkerEventLoopGroup(bossGroup, configuration.EventLoopCount);
            serverBufferAllocator = new PooledByteBufferAllocator();
            ServerBootstrap bootstrap = new ServerBootstrap();
            bootstrap.Group(bossGroup, workerGroup);
            bootstrap.Channel<TcpServerChannel>();
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux)
                || RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                bootstrap
                    .Option(ChannelOption.SoReuseport, true)
                    .ChildOption(ChannelOption.SoReuseaddr, true);
            }
            bootstrap
                    .Option(ChannelOption.SoBacklog, 8192)
                    .ChildOption(ChannelOption.Allocator, serverBufferAllocator)
                    .ChildHandler(new ActionChannelInitializer<IChannel>(channel =>
                    {
                        IChannelPipeline pipeline = channel.Pipeline;
                        pipeline.AddLast(new HttpServerCodec());
                        pipeline.AddLast(new CorsHandler(CorsConfigBuilder
                                     .ForAnyOrigin()
                                     .AllowNullOrigin()
                                     .AllowedRequestMethods(HttpMethod.Get, HttpMethod.Post, HttpMethod.Options, HttpMethod.Delete)
                                     .AllowedRequestHeaders((AsciiString)"origin", (AsciiString)"range", (AsciiString)"accept-encoding", (AsciiString)"referer", (AsciiString)"Cache-Control", (AsciiString)"X-Proxy-Authorization", (AsciiString)"X-Requested-With", (AsciiString)"Content-Type")
                                     .ExposeHeaders((StringCharSequence)"Server", (StringCharSequence)"range", (StringCharSequence)"Content-Length", (StringCharSequence)"Content-Range")
                                     .AllowCredentials()
                                     .Build()));
                        pipeline.AddLast(new HttpObjectAggregator(int.MaxValue));
                        using (var scope = serviceProvider.CreateScope())
                        {
                            pipeline.AddLast("JT1078HttpServerHandler", scope.ServiceProvider.GetRequiredService<JT1078HttpServerHandler>());
                        }
                    }));
            logger.LogInformation($"JT1078 Http Server start at {IPAddress.Any}:{configuration.HttpPort}.");
            return bootstrap.BindAsync(configuration.HttpPort)
                .ContinueWith(i => bootstrapChannel = i.Result);
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            await bootstrapChannel.CloseAsync();
            var quietPeriod = configuration.QuietPeriodTimeSpan;
            var shutdownTimeout = configuration.ShutdownTimeoutTimeSpan;
            await workerGroup.ShutdownGracefullyAsync(quietPeriod, shutdownTimeout);
            await bossGroup.ShutdownGracefullyAsync(quietPeriod, shutdownTimeout);
        }
    }
}
