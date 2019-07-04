using DotNetty.Buffers;
using DotNetty.Codecs;
using DotNetty.Handlers.Timeout;
using DotNetty.Transport.Bootstrapping;
using DotNetty.Transport.Channels;
using DotNetty.Transport.Libuv;
using JT1078.DotNetty.Core.Codecs;
using JT1078.DotNetty.Core.Configurations;
using JT1078.DotNetty.Tcp.Handlers;
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

namespace JT1078.DotNetty.Tcp
{
    /// <summary>
    /// JT1078 Tcp网关服务
    /// </summary>
    internal class JT1078TcpServerHost : IHostedService
    {
        private readonly IServiceProvider serviceProvider;
        private readonly JT1078Configuration configuration;
        private readonly ILogger<JT1078TcpServerHost> logger;
        private DispatcherEventLoopGroup bossGroup;
        private WorkerEventLoopGroup workerGroup;
        private IChannel bootstrapChannel;
        private IByteBufferAllocator serverBufferAllocator;

        public JT1078TcpServerHost(
            IServiceProvider provider,
            ILoggerFactory loggerFactory,
            IOptions<JT1078Configuration> configurationAccessor)
        {
            serviceProvider = provider;
            configuration = configurationAccessor.Value;
            logger=loggerFactory.CreateLogger<JT1078TcpServerHost>();
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
               .Option(ChannelOption.SoBacklog, configuration.SoBacklog)
               .ChildOption(ChannelOption.Allocator, serverBufferAllocator)
               .ChildHandler(new ActionChannelInitializer<IChannel>(channel =>
               {
                   IChannelPipeline pipeline = channel.Pipeline;
                   using (var scope = serviceProvider.CreateScope())
                   {
                       channel.Pipeline.AddLast("JT1078TcpBuffer", new DelimiterBasedFrameDecoder(int.MaxValue,true,
                                    Unpooled.CopiedBuffer(JT1078Package.FH_Bytes)));
                       channel.Pipeline.AddLast("JT1078TcpDecode", scope.ServiceProvider.GetRequiredService<JT1078TcpDecoder>());
                       channel.Pipeline.AddLast("JT1078SystemIdleState", new IdleStateHandler(
                                                configuration.ReaderIdleTimeSeconds,
                                                configuration.WriterIdleTimeSeconds,
                                                configuration.AllIdleTimeSeconds));
                       channel.Pipeline.AddLast("JT1078TcpConnection", scope.ServiceProvider.GetRequiredService<JT1078TcpConnectionHandler>());
                       channel.Pipeline.AddLast("JT1078TcpService", scope.ServiceProvider.GetRequiredService<JT1078TcpServerHandler>());
                   }
               }));
            logger.LogInformation($"JT1078 TCP Server start at {IPAddress.Any}:{configuration.TcpPort}.");
            return bootstrap.BindAsync(configuration.TcpPort)
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
