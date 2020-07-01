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
            Logger = loggerFactory.CreateLogger<JT1078TcpServer>();
            Configuration = jT1078ConfigurationAccessor.Value;
            this.jT1078MsgProducer = jT1078MsgProducer;
            InitServer();
        }

        private void InitServer()
        {
            var IPEndPoint = new System.Net.IPEndPoint(IPAddress.Any, Configuration.UdpPort);
            server = new Socket(IPEndPoint.AddressFamily, SocketType.Dgram, ProtocolType.Udp);
            //server.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.NoDelay, true);
            //server.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReceiveBuffer, Configuration.MiniNumBufferSize);
            //server.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.SendBuffer, Configuration.MiniNumBufferSize);
            server.Bind(IPEndPoint);
        }
        public Task StartAsync(CancellationToken cancellationToken)
        {
            Logger.LogInformation($"JT1078 Udp Server start at {IPAddress.Any}:{Configuration.UdpPort}.");
            Task.Factory.StartNew(async () =>
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    JT1078UdpSession session = new JT1078UdpSession(server);
                    var pipe = new Pipe();
                   Task writing = FillPipeAsync(pipe.Writer, session);
                    Task reading = ReadPipeAsync(pipe.Reader, session);
                    await Task.WhenAll(reading, writing);
                }
            }, cancellationToken);
            return Task.CompletedTask;
        }
        private async Task FillPipeAsync(PipeWriter writer, JT1078UdpSession session)
        {
            while (true)
            {
                try
                {
                   await Task.Factory.StartNew(() => {
                       //设备多久没发数据就断开连接 Receive Timeout.
                       EndPoint remoteIp = new IPEndPoint(IPAddress.Any, 0);//用来保存发送方的ip和端口号;
                       var buffer = new byte[Configuration.MiniNumBufferSize];
                       var bytesRead = server.ReceiveFrom(buffer, ref remoteIp);
                       session.RemoteEndPoint = remoteIp;
                       SessionManager.TryAdd(session);
                       Memory<byte> memory = writer.GetMemory(Configuration.MiniNumBufferSize);
                       buffer.ToArray().CopyTo(memory);
                       if (bytesRead == 0)
                       {
                           return;
                       }
                       writer.Advance(bytesRead);
                   });
                }
                catch (AggregateException ex) {

                    break;
                }
                catch (OperationCanceledException ex)
                {
                    //  Logger.LogError($"[Receive Timeout]:{session.Client.RemoteEndPoint}");
                    break;
                }
                catch (System.Net.Sockets.SocketException ex)
                {
                    // Logger.LogError($"[{ex.SocketErrorCode.ToString()},{ex.Message}]:{session.Client.RemoteEndPoint}");
                    break;
                }
#pragma warning disable CA1031 // Do not catch general exception types
                catch (Exception ex)
                {
                    // Logger.LogError(ex, $"[Receive Error]:{session.Client.RemoteEndPoint}");
                    break;
                }
#pragma warning restore CA1031 // Do not catch general exception types
                FlushResult result = await writer.FlushAsync();
                if (result.IsCompleted)
                {
                    break;
                }
            }
            writer.Complete();
        }
        private async Task ReadPipeAsync(PipeReader reader, JT1078UdpSession session)
        {
            while (true)
            {
                ReadResult result = await reader.ReadAsync();
                if (result.IsCompleted)
                {
                    break;
                }
                ReadOnlySequence<byte> buffer = result.Buffer;
                SequencePosition consumed = buffer.Start;
                SequencePosition examined = buffer.End;
                try
                {
                    if (result.IsCanceled) break;
                    if (buffer.Length > 0)
                    {
                        ReaderBuffer(ref buffer, session, out consumed, out examined);
                    }
                }
#pragma warning disable CA1031 // Do not catch general exception types
                catch (Exception ex)
                {
                   // Logger.LogError(ex, $"[ReadPipe Error]:{session.Client.RemoteEndPoint}");
                    break;
                }
#pragma warning restore CA1031 // Do not catch general exception types
                finally
                {
                    reader.AdvanceTo(consumed, examined);
                }
            }
            reader.Complete();
        }
        private void ReaderBuffer(ref ReadOnlySequence<byte> buffer, JT1078UdpSession session, out SequencePosition consumed, out SequencePosition examined)
        {
            consumed = buffer.Start;
            examined = buffer.End;
            if (buffer.Length < 15)
            {
                throw new ArgumentException("not JT1078 package");
            }
            SequenceReader<byte> seqReader = new SequenceReader<byte>(buffer);
            long totalConsumed = 0;
            while (!seqReader.End)
            {
                if ((seqReader.Length - seqReader.Consumed) < 15)
                {
                    throw new ArgumentException("not JT1078 package");
                }
                var header = seqReader.Sequence.Slice(seqReader.Consumed, 4);
                var headerValue = BinaryPrimitives.ReadUInt32BigEndian(header.FirstSpan);
                if (JT1078Package.FH == headerValue)
                {
                    //sim
                    var sim = ReadBCD(seqReader.Sequence.Slice(seqReader.Consumed + 8, 6).FirstSpan, 12);
                    //根据数据类型处理对应的数据长度
                    seqReader.Advance(15);
                    if (seqReader.TryRead(out byte dataType))
                    {
                        JT1078Label3 label3 = new JT1078Label3(dataType);
                        int bodyLength = 0;
                        //透传的时候没有该字段
                        if (label3.DataType != JT1078DataType.透传数据)
                        {
                            //时间戳
                            bodyLength += 8;
                        }
                        //非视频帧时没有该字段
                        if (label3.DataType == JT1078DataType.视频I帧 ||
                            label3.DataType == JT1078DataType.视频P帧 ||
                            label3.DataType == JT1078DataType.视频B帧)
                        {
                            //上一个关键帧 + 上一帧 = 2 + 2
                            bodyLength += 4;
                        }
                        seqReader.Advance(bodyLength);
                        var bodyLengthFirstSpan = seqReader.Sequence.Slice(seqReader.Consumed, 2).FirstSpan;
                        //数据体长度
                        seqReader.Advance(2);
                        bodyLength = BinaryPrimitives.ReadUInt16BigEndian(bodyLengthFirstSpan);
                        //数据体
                        seqReader.Advance(bodyLength);
                        if (string.IsNullOrEmpty(sim))
                        {
                            sim = session.SessionID;
                        }
                        SessionManager.TryLink(sim, session);
                        var package = seqReader.Sequence.Slice(totalConsumed, seqReader.Consumed - totalConsumed);
                        try
                        {
                            if (jT1078UseType == JT1078UseType.Queue)
                            {
                                jT1078MsgProducer.ProduceAsync(sim, package.ToArray());
                            }
                            else
                            {
                                jT1078PackageProducer.ProduceAsync(sim, JT1078Serializer.Deserialize(package.FirstSpan));
                            }
                        }
                        catch (Exception ex)
                        {
                            Logger.LogError(ex, $"[Parse]:{package.ToArray().ToHexString()}");
                        }
                        totalConsumed += (seqReader.Consumed - totalConsumed);
                        if (seqReader.End) break;
                    }
                }
            }
            if (seqReader.Length == totalConsumed)
            {
                examined = consumed = buffer.End;
            }
            else
            {
                consumed = buffer.GetPosition(totalConsumed);
            }
        }
        public Task StopAsync(CancellationToken cancellationToken)
        {
            Logger.LogInformation("JT1078 Tcp Server Stop");
            if (server?.Connected ?? false)
                server.Shutdown(SocketShutdown.Both);
            server?.Close();
            return Task.CompletedTask;
        }
        string ReadBCD(ReadOnlySpan<byte> readOnlySpan, int len)
        {
            int count = len / 2;
            StringBuilder bcdSb = new StringBuilder(count);
            for (int i = 0; i < count; i++)
            {
                bcdSb.Append(readOnlySpan[i].ToString("X2"));
            }
            return bcdSb.ToString().TrimStart('0');
        }
    }
}