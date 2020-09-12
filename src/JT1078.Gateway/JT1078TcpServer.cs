using System;
using System.Buffers;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO.Pipelines;
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
    public class JT1078TcpServer : IHostedService
    {
        private Socket server;

        private readonly ILogger Logger;

        private readonly JT1078Configuration Configuration;

        private readonly JT1078SessionManager SessionManager;

        private readonly IJT1078PackageProducer  jT1078PackageProducer;

        private readonly IJT1078MsgProducer  jT1078MsgProducer;

        private readonly JT1078UseType jT1078UseType;

        /// <summary>
        /// 使用正常方式
        /// </summary>
        /// <param name="jT1078PackageProducer"></param>
        /// <param name="jT1078ConfigurationAccessor"></param>
        /// <param name="loggerFactory"></param>
        /// <param name="jT1078SessionManager"></param>
        public JT1078TcpServer(
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
        public JT1078TcpServer(
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
            var IPEndPoint = new System.Net.IPEndPoint(IPAddress.Any, Configuration.TcpPort);
            server = new Socket(IPEndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            server.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.NoDelay, true);
            server.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true);
            server.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReceiveBuffer, Configuration.MiniNumBufferSize);
            server.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.SendBuffer, Configuration.MiniNumBufferSize);
            server.LingerState = new LingerOption(false, 0);
            server.Bind(IPEndPoint);
            server.Listen(Configuration.SoBacklog);
        }
        public Task StartAsync(CancellationToken cancellationToken)
        {
            Logger.LogInformation($"JT1078 Tcp Server start at {IPAddress.Any}:{Configuration.TcpPort}.");
            Task.Factory.StartNew(async () =>
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    var socket = await server.AcceptAsync();
                    JT1078TcpSession jT808TcpSession = new JT1078TcpSession(socket);
                    SessionManager.TryAdd(jT808TcpSession);
                    await Task.Factory.StartNew(async (state) =>
                    {
                        var session = (JT1078TcpSession)state;
                        if (Logger.IsEnabled(LogLevel.Information))
                        {
                            Logger.LogInformation($"[Connected]:{session.Client.RemoteEndPoint}");
                        }
                        var pipe = new Pipe();
                        Task writing = FillPipeAsync(session, pipe.Writer);
                        Task reading = ReadPipeAsync(session, pipe.Reader);
                        await Task.WhenAll(reading, writing);
                        SessionManager.RemoveBySessionId(session.SessionID);
                    }, jT808TcpSession);
                }
            }, cancellationToken);
            return Task.CompletedTask;
        }
        private async Task FillPipeAsync(JT1078TcpSession session, PipeWriter writer)
        {
            while (true)
            {
                try
                {
                    Memory<byte> memory = writer.GetMemory(Configuration.MiniNumBufferSize);
                    //设备多久没发数据就断开连接 Receive Timeout.
                    int bytesRead = await session.Client.ReceiveAsync(memory, SocketFlags.None, session.ReceiveTimeout.Token);
                    if (bytesRead == 0)
                    {
                        break;
                    }
                    writer.Advance(bytesRead);
                }
                catch (System.ObjectDisposedException ex)
                {

                }
                catch (OperationCanceledException ex)
                {
                    Logger.LogError($"[Receive Timeout]:{session.Client.RemoteEndPoint}");
                    break;
                }
                catch (System.Net.Sockets.SocketException ex)
                {
                    Logger.LogError($"[{ex.SocketErrorCode.ToString()},{ex.Message}]:{session.Client.RemoteEndPoint}");
                    break;
                }
#pragma warning disable CA1031 // Do not catch general exception types
                catch (Exception ex)
                {
                    Logger.LogError(ex, $"[Receive Error]:{session.Client.RemoteEndPoint}");
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
        private async Task ReadPipeAsync(JT1078TcpSession session, PipeReader reader)
        {
            FixedHeaderInfo fixedHeaderInfo = new FixedHeaderInfo();
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
                        ReaderBuffer(ref buffer, fixedHeaderInfo, session, out consumed, out examined);
                    }
                }
#pragma warning disable CA1031 // Do not catch general exception types
                catch (Exception ex)
                {
                    Logger.LogError(ex, $"[ReadPipe Error]:{session.Client.RemoteEndPoint}");
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
        private void ReaderBuffer(ref ReadOnlySequence<byte> buffer, FixedHeaderInfo fixedHeaderInfo, JT1078TcpSession session, out SequencePosition consumed, out SequencePosition examined)
        {
            consumed = buffer.Start;
            examined = buffer.End;
            SequenceReader<byte> seqReader = new SequenceReader<byte>(buffer);
            long totalConsumed = 0;
            while (!seqReader.End)
            {
                if (!fixedHeaderInfo.FoundHeader)
                {
                    if (seqReader.Length < 4)
                        throw new ArgumentException("not JT1078 package");
                    var header = seqReader.Sequence.Slice(seqReader.Consumed, 4);
                    var headerValue = BinaryPrimitives.ReadUInt32BigEndian(header.FirstSpan);
                    if (JT1078Package.FH == headerValue)
                    {
                        //sim
                        fixedHeaderInfo.SIM = ReadBCD(seqReader.Sequence.Slice(seqReader.Consumed + 8, 6).FirstSpan, 12);
                        if (string.IsNullOrEmpty(fixedHeaderInfo.SIM))
                        {
                            fixedHeaderInfo.SIM = session.SessionID;
                        }
                        //根据数据类型处理对应的数据长度
                        fixedHeaderInfo.TotalSize += 15;
                        var dataType = seqReader.Sequence.Slice(seqReader.Consumed+fixedHeaderInfo.TotalSize, 1).FirstSpan[0];
                        fixedHeaderInfo.TotalSize += 1;
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
                        fixedHeaderInfo.TotalSize += bodyLength;
                        var bodyLengthFirstSpan = seqReader.Sequence.Slice(seqReader.Consumed + fixedHeaderInfo.TotalSize, 2).FirstSpan;
                        //数据体长度
                        fixedHeaderInfo.TotalSize += 2;
                        bodyLength = BinaryPrimitives.ReadUInt16BigEndian(bodyLengthFirstSpan);
                        if (bodyLength == 0)//数据体长度为0
                        {
                            seqReader.Advance(fixedHeaderInfo.TotalSize);
                            var package1 = seqReader.Sequence.Slice(totalConsumed, seqReader.Consumed - totalConsumed);
                            try
                            {
                                SessionManager.TryLink(fixedHeaderInfo.SIM, session);
                                if (jT1078UseType == JT1078UseType.Queue)
                                {
                                    jT1078MsgProducer.ProduceAsync(fixedHeaderInfo.SIM, package1.FirstSpan.ToArray());
                                }
                                else
                                {
                                    jT1078PackageProducer.ProduceAsync(fixedHeaderInfo.SIM, JT1078Serializer.Deserialize(package1.FirstSpan));
                                }
                            }
                            catch (Exception ex)
                            {
                                Logger.LogError(ex, $"[Parse]:{package1.ToArray().ToHexString()}");
                            }
                            finally
                            {
                                totalConsumed += (seqReader.Consumed - totalConsumed);
                                fixedHeaderInfo.Reset();
                            }
                            continue;
                        }
                        //数据体
                        fixedHeaderInfo.TotalSize += bodyLength;
                        fixedHeaderInfo.FoundHeader = true;
                    }
                }
                if (seqReader.Length < fixedHeaderInfo.TotalSize) break;
                seqReader.Advance(fixedHeaderInfo.TotalSize);
                var package = seqReader.Sequence.Slice(totalConsumed, seqReader.Consumed - totalConsumed);
                try
                {
                    SessionManager.TryLink(fixedHeaderInfo.SIM, session);
                    if (jT1078UseType == JT1078UseType.Queue)
                    {
                        jT1078MsgProducer.ProduceAsync(fixedHeaderInfo.SIM, package.ToArray());
                    }
                    else
                    {
                        jT1078PackageProducer.ProduceAsync(fixedHeaderInfo.SIM, JT1078Serializer.Deserialize(package.FirstSpan));
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, $"[Parse]:{package.ToArray().ToHexString()}");
                }
                finally
                {
                    totalConsumed += (seqReader.Consumed - totalConsumed);
                    fixedHeaderInfo.Reset();
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

        class FixedHeaderInfo
        {
            public bool FoundHeader { get; set; }
            public int TotalSize { get; set; }
            public string SIM { get; set; }
            public void Reset()
            {
                FoundHeader = false;
                TotalSize = 0;
            }
        }
    }
}
