using System;
using System.Buffers;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO.Pipelines;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using JT1078.Gateway.Abstractions;
using JT1078.Gateway.Abstractions.Enums;
using JT1078.Gateway.Configurations;
using JT1078.Gateway.Sessions;
using JT1078.Protocol;
using JT1078.Protocol.Enums;
using JT1078.Protocol.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace JT1078.Gateway
{
    public class JT1078UdpServer : IHostedService
    {
        private Socket server;

        private readonly ILogger Logger;

        private readonly JT1078Configuration Configuration;

        private readonly JT1078SessionManager SessionManager;

        private readonly IJT1078PackageProducer jT1078PackageProducer;

        private readonly IJT1078MsgProducer jT1078MsgProducer;

        private readonly JT1078UseType jT1078UseType;

        /// <summary>
        /// 使用正常方式
        /// </summary>
        /// <param name="jT1078PackageProducer"></param>
        /// <param name="jT1078ConfigurationAccessor"></param>
        /// <param name="loggerFactory"></param>
        /// <param name="jT1078SessionManager"></param>
        public JT1078UdpServer(
                IJT1078PackageProducer jT1078PackageProducer,
                IOptions<JT1078Configuration> jT1078ConfigurationAccessor,
                ILoggerFactory loggerFactory,
                JT1078SessionManager jT1078SessionManager)
        {
            SessionManager = jT1078SessionManager;
            jT1078UseType = JT1078UseType.Normal;
            Logger = loggerFactory.CreateLogger<JT1078TcpServer>();
            Configuration = jT1078ConfigurationAccessor.Value;
            this.jT1078PackageProducer = jT1078PackageProducer;
            InitServer();
        }

        /// <summary>
        /// 使用队列方式
        /// </summary>
        /// <param name="jT1078MsgProducer"></param>
        /// <param name="jT1078ConfigurationAccessor"></param>
        /// <param name="loggerFactory"></param>
        /// <param name="jT1078SessionManager"></param>
        public JT1078UdpServer(
                 IJT1078MsgProducer jT1078MsgProducer,
                IOptions<JT1078Configuration> jT1078ConfigurationAccessor,
                ILoggerFactory loggerFactory,
                JT1078SessionManager jT1078SessionManager)
        {
            SessionManager = jT1078SessionManager;
            jT1078UseType = JT1078UseType.Queue;
            Logger = loggerFactory.CreateLogger<JT1078UdpServer>();
            Configuration = jT1078ConfigurationAccessor.Value;
            this.jT1078MsgProducer = jT1078MsgProducer;
            InitServer();
        }

        private void InitServer()
        {
            var IPEndPoint = new System.Net.IPEndPoint(IPAddress.Any, Configuration.UdpPort);
            server = new Socket(IPEndPoint.AddressFamily, SocketType.Dgram, ProtocolType.Udp);
            server.Bind(IPEndPoint);
        }
        public Task StartAsync(CancellationToken cancellationToken)
        {
            Logger.LogInformation($"JT1078 Udp Server start at {IPAddress.Any}:{Configuration.UdpPort}.");
            Task.Factory.StartNew(async () =>
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    var buffer = ArrayPool<byte>.Shared.Rent(Configuration.MiniNumBufferSize);
                    try
                    {
                        var segment = new ArraySegment<byte>(buffer);
                        var result = await server.ReceiveMessageFromAsync(segment, SocketFlags.None, server.LocalEndPoint);
                        ReaderBuffer(buffer.AsSpan(0, result.ReceivedBytes), server, result);
                    }
                    catch (AggregateException ex)
                    {
                        Logger.LogError(ex, "Receive MessageFrom Async");
                    }
#pragma warning disable CA1031 // Do not catch general exception types
                    catch (Exception ex)
                    {
                        Logger.LogError(ex, $"Received Bytes");
                    }
#pragma warning restore CA1031 // Do not catch general exception types
                    finally
                    {
                        ArrayPool<byte>.Shared.Return(buffer);
                    }
                }
            }, cancellationToken);
            return Task.CompletedTask;
        }
        private void ReaderBuffer(ReadOnlySpan<byte> buffer, Socket socket, SocketReceiveMessageFromResult receiveMessageFromResult)
        {
            try
            {
                var package = JT1078Serializer.Deserialize(buffer);
                if (Logger.IsEnabled(LogLevel.Trace)) Logger.LogTrace($"[Accept Hex {receiveMessageFromResult.RemoteEndPoint}]:{buffer.ToArray().ToHexString()}");
                var session = SessionManager.TryLink(package.SIM, socket, receiveMessageFromResult.RemoteEndPoint);
                if (Logger.IsEnabled(LogLevel.Information))
                {
                    Logger.LogInformation($"[Connected]:{receiveMessageFromResult.RemoteEndPoint}");
                }
                if (jT1078UseType == JT1078UseType.Queue)
                {
                    jT1078MsgProducer.ProduceAsync(package.SIM, buffer.ToArray());
                }
                else
                {
                    jT1078PackageProducer.ProduceAsync(package.SIM, package);
                }
            }
            catch (NotImplementedException ex)
            {
                Logger.LogError(ex.Message);
            }
#pragma warning disable CA1031 // Do not catch general exception types
            catch (Exception ex)
            {
                Logger.LogError(ex, $"[ReaderBuffer]:{ buffer.ToArray().ToHexString()}");
            }
#pragma warning restore CA1031 // Do not catch general exception types
        }
        public Task StopAsync(CancellationToken cancellationToken)
        {
            Logger.LogInformation("JT1078 Udp Server Stop");
            if (server?.Connected ?? false)
                server.Shutdown(SocketShutdown.Both);
            server?.Close();
            return Task.CompletedTask;
        }
    }
}