using System;
using System.Buffers;
using System.Buffers.Binary;
using System.IO.Pipelines;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using JT1078.Gateway.Abstractions;
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

        private readonly ILogger LogLogger;

        private readonly JT1078Configuration Configuration;

        private readonly JT1078SessionManager SessionManager;

        private readonly IJT1078MsgProducer jT1078MsgProducer;

        private readonly JT1078UseType jT1078UseType;

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
            Logger = loggerFactory.CreateLogger<JT1078TcpServer>();
            LogLogger = loggerFactory.CreateLogger("JT1078.Gateway.JT1078Logging");
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
                if (seqReader.Length < 30)
                {
                    fixedHeaderInfo.Reset();
                    break;
                }
                if (!fixedHeaderInfo.FoundHeader)
                {
                    var header = seqReader.Sequence.Slice(0, 4);
                    uint headerValue = BinaryPrimitives.ReadUInt32BigEndian(header.ToArray());
                    if (JT1078Package.FH == headerValue)
                    {
                        //sim
                        if (string.IsNullOrEmpty(fixedHeaderInfo.SIM))
                        {
                            fixedHeaderInfo.SIM = ReadBCD(seqReader.Sequence.Slice(8, 6).ToArray(), 12);
                            fixedHeaderInfo.SIM = fixedHeaderInfo.SIM ?? session.SessionID;
                        }
                        //根据数据类型处理对应的数据长度
                        fixedHeaderInfo.TotalSize += 15;
                        var dataType = seqReader.Sequence.Slice(fixedHeaderInfo.TotalSize, 1).FirstSpan[0];
                        fixedHeaderInfo.TotalSize += 1;
                        int bodyLength = GetRealDataBodyLength(dataType);
                        fixedHeaderInfo.TotalSize += bodyLength;
                        var bodyLengthFirstSpan = seqReader.Sequence.Slice(fixedHeaderInfo.TotalSize, 2).ToArray();
                        fixedHeaderInfo.TotalSize += 2;
                        //数据体长度
                        bodyLength = BinaryPrimitives.ReadUInt16BigEndian(bodyLengthFirstSpan);
                        if (bodyLength < 0)
                        {
                            fixedHeaderInfo.Reset();
                            throw new ArgumentException("jt1078 package body length Error.");
                        }
                        if (bodyLength == 0)//数据体长度为0
                        {
                            var package1 = seqReader.Sequence.Slice(0, fixedHeaderInfo.TotalSize).ToArray();
                            seqReader.Advance(fixedHeaderInfo.TotalSize);
                            if (LogLogger.IsEnabled(LogLevel.Trace))
                            {
                                LogLogger.LogTrace($"{package1.ToHexString()}");
                            }
                            try
                            {
                                SessionManager.TryLink(fixedHeaderInfo.SIM, session);
                                jT1078MsgProducer.ProduceAsync(fixedHeaderInfo.SIM, package1.ToArray());
                            }
                            catch (Exception ex)
                            {
                                LogLogger.LogError($"[Error Parse 1]:{package1.ToHexString()}");
                                Logger.LogError(ex, $"[Error Parse 1]:{package1.ToHexString()}");
                            }
                            finally
                            {
                                totalConsumed += seqReader.Consumed;
                                seqReader = new SequenceReader<byte>(seqReader.Sequence.Slice(fixedHeaderInfo.TotalSize));
                                fixedHeaderInfo.Reset();
#if DEBUG
                                Interlocked.Increment(ref Counter);
#endif
                            }
                            continue;
                        }
                        //数据体
                        fixedHeaderInfo.TotalSize += bodyLength;
                        fixedHeaderInfo.FoundHeader = true;
                    }
                    else
                    {
                        fixedHeaderInfo.Reset();
                        throw new ArgumentException("not JT1078 package.");
                    }
                }
                if ((seqReader.Length - fixedHeaderInfo.TotalSize) < 0) break;
                var package = seqReader.Sequence.Slice(0, fixedHeaderInfo.TotalSize).ToArray();
                seqReader.Advance(fixedHeaderInfo.TotalSize);
                if (LogLogger.IsEnabled(LogLevel.Trace))
                {
                    LogLogger.LogTrace($"===>{package.ToHexString()}");
                }
                try
                {
                    SessionManager.TryLink(fixedHeaderInfo.SIM, session);
                    jT1078MsgProducer.ProduceAsync(fixedHeaderInfo.SIM, package);
                }
                catch (Exception ex)
                {
                    LogLogger.LogError($"[Error Parse 2]:{package.ToHexString()}");
                    Logger.LogError(ex, $"[Error Parse 2]:{package.ToHexString()}");
                }
                finally
                {
                    totalConsumed += seqReader.Consumed;
                    seqReader = new SequenceReader<byte>(seqReader.Sequence.Slice(fixedHeaderInfo.TotalSize));
                    fixedHeaderInfo.Reset();
#if DEBUG
                    Interlocked.Increment(ref Counter);
#endif
                }
#if DEBUG
                if (Logger.IsEnabled(LogLevel.Trace))
                {
                    Logger.LogTrace($"======>{Counter}");
                }
#endif
            }
            if (seqReader.End)
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
            SessionManager.GetTcpAll().ForEach(session =>
            {
                try
                {
                    session.Close();
                }
                catch (Exception ex)
                {

                }
            });
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
        int GetRealDataBodyLength(byte dataType)
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
            return bodyLength;
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
