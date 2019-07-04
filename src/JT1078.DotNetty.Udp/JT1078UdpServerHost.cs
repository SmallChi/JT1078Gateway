using DotNetty.Transport.Bootstrapping;
using DotNetty.Transport.Channels;
using DotNetty.Transport.Channels.Sockets;
using JT1078.DotNetty.Core.Codecs;
using JT1078.DotNetty.Core.Configurations;
using JT1078.DotNetty.Udp.Handlers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Net;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace JT1078.DotNetty.Udp
{
    /// <summary>
    /// JT1078 Udp网关服务
    /// </summary>
    internal class JT1078UdpServerHost : IHostedService
    {
        private readonly IServiceProvider serviceProvider;
        private readonly JT1078Configuration configuration;
        private readonly ILogger<JT1078UdpServerHost> logger;
        private MultithreadEventLoopGroup group;
        private IChannel bootstrapChannel;

        public JT1078UdpServerHost(
            IServiceProvider provider,
            ILoggerFactory loggerFactory,
            IOptions<JT1078Configuration> jT808ConfigurationAccessor)
        {
            serviceProvider = provider;
            configuration = jT808ConfigurationAccessor.Value;
            logger=loggerFactory.CreateLogger<JT1078UdpServerHost>();
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            group = new MultithreadEventLoopGroup();
            Bootstrap bootstrap = new Bootstrap();
            bootstrap.Group(group);
            bootstrap.Channel<SocketDatagramChannel>();
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux)
                || RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                bootstrap
                    .Option(ChannelOption.SoReuseport, true);
            }
            bootstrap
               .Option(ChannelOption.SoBroadcast, true)
               .Handler(new ActionChannelInitializer<IChannel>(channel =>
               {
                   IChannelPipeline pipeline = channel.Pipeline;
                   using (var scope = serviceProvider.CreateScope())
                   {   
                       pipeline.AddLast("JT1078UdpDecoder", scope.ServiceProvider.GetRequiredService<JT1078UdpDecoder>());
                       pipeline.AddLast("JT1078UdpService", scope.ServiceProvider.GetRequiredService<JT1078UdpServerHandler>());
                   }
               }));
            logger.LogInformation($"JT1078 Udp Server start at {IPAddress.Any}:{configuration.UdpPort}.");
            return bootstrap.BindAsync(configuration.UdpPort)
                .ContinueWith(i => bootstrapChannel = i.Result);
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            await bootstrapChannel.CloseAsync();
            var quietPeriod = configuration.QuietPeriodTimeSpan;
            var shutdownTimeout = configuration.ShutdownTimeoutTimeSpan;
            await group.ShutdownGracefullyAsync(quietPeriod, shutdownTimeout);
        }
    }
}
