using DotNetty.Buffers;
using DotNetty.Codecs;
using DotNetty.Codecs.Http;
using DotNetty.Handlers.Streams;
using DotNetty.Handlers.Timeout;
using DotNetty.Transport.Bootstrapping;
using DotNetty.Transport.Channels;
using DotNetty.Transport.Libuv;
using JT1078.DotNetty.Core.Codecs;
using JT1078.DotNetty.Core.Configurations;
using JT1078.DotNetty.WebSocket.Handlers;
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

namespace JT1078.DotNetty.WebSocket
{
    /// <summary>
    /// JT1078 WebSocket服务
    /// </summary>
    internal class JT1078WebSocketServerHost : IHostedService
    {
        private readonly JT1078Configuration configuration;
        private readonly ILogger<JT1078WebSocketServerHost> logger;
        private DispatcherEventLoopGroup bossGroup;
        private WorkerEventLoopGroup workerGroup;
        private IChannel bootstrapChannel;
        private IByteBufferAllocator serverBufferAllocator;
        private readonly IServiceProvider serviceProvider;
        public JT1078WebSocketServerHost(
            IServiceProvider serviceProvider,
            ILoggerFactory loggerFactory,
            IOptions<JT1078Configuration> configurationAccessor)
        {
            this.serviceProvider = serviceProvider;
            configuration = configurationAccessor.Value;
            logger=loggerFactory.CreateLogger<JT1078WebSocketServerHost>();
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
                        pipeline.AddLast(new HttpObjectAggregator(65536));
                        using (var scope = serviceProvider.CreateScope())
                        {
                            pipeline.AddLast("JT1078WebSocketServerHandler", scope.ServiceProvider.GetRequiredService<JT1078WebSocketServerHandler>());
                        }
                    }));
            logger.LogInformation($"JT1078 WebSocket Server start at {IPAddress.Any}:{configuration.WebSocketPort}.");
            return bootstrap.BindAsync(configuration.WebSocketPort)
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
