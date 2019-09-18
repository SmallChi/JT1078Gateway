using DotNetty.Buffers;
using DotNetty.Codecs.Http.WebSockets;
using JT1078.DotNetty.Core.Session;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using JT1078.Protocol;
using System.Collections.Concurrent;
using JT1078.Protocol.Enums;
using System.Diagnostics;
using System.IO.Pipes;
using Newtonsoft.Json;
using DotNetty.Common.Utilities;
using DotNetty.Codecs.Http;
using DotNetty.Handlers.Streams;
using DotNetty.Transport.Channels;

namespace JT1078.DotNetty.TestHosting
{
    class FFMPEGHTTPFLVHostedService : IHostedService,IDisposable
    {
        private readonly Process process;
        private readonly NamedPipeServerStream pipeServerOut;
        private const string PipeNameOut = "demo1serverout";
        private static readonly AsciiString ServerName = AsciiString.Cached("JT1078Netty");
        private static readonly AsciiString DateEntity = HttpHeaderNames.Date;
        private static readonly AsciiString ServerEntity = HttpHeaderNames.Server;
        private readonly JT1078HttpSessionManager jT1078HttpSessionManager;
        /// <summary>
        /// 需要缓存flv的第一包数据，当新用户进来先推送第一包的数据
        /// </summary>
        private byte[] flvFirstPackage;
        private ConcurrentDictionary<string, byte> exists = new ConcurrentDictionary<string, byte>();
        public FFMPEGHTTPFLVHostedService(JT1078HttpSessionManager jT1078HttpSessionManager)
        {
            pipeServerOut = new NamedPipeServerStream(PipeNameOut, PipeDirection.In, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous,10240,10240);
            process = new Process
            {
                StartInfo =
                {
                    FileName = @"C:\ffmpeg\bin\ffmpeg.exe",
                    Arguments = $@"-f dshow -i video={HardwareCamera.CameraName} -c copy -f flv -vcodec h264 -y \\.\pipe\{PipeNameOut}",
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };
            this.jT1078HttpSessionManager = jT1078HttpSessionManager;
        }


        public void Dispose()
        {
            pipeServerOut.Dispose();
        }


        public byte[] Chunk(byte[] data)
        {
            byte[] buffer =new byte[4+2+2+ data.Length];
            buffer[0] = (byte)(data.Length >> 24);
            buffer[1] = (byte)(data.Length >> 16);
            buffer[2] = (byte)(data.Length >> 8);
            buffer[3] = (byte)data.Length;
            buffer[4]=(byte)'\r';
            buffer[5] = (byte)'\n';
            Array.Copy(data,0, buffer, 7,data.Length);
            buffer[buffer.Length - 2] = (byte)'\r';
            buffer[buffer.Length - 1] = (byte)'\n';
            return buffer;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            process.Start();
            Task.Run(() =>
            {
                while (true)
                {
                    try
                    {
                        Console.WriteLine("IsConnected>>>" + pipeServerOut.IsConnected);
                        if (pipeServerOut.IsConnected)
                        {
                            if (pipeServerOut.CanRead)
                            {
                                Span<byte> v1 = new byte[2048];
                                var length = pipeServerOut.Read(v1);
                                var realValue = v1.Slice(0, length).ToArray();
                                if (realValue.Length <= 0) continue;
                                if (flvFirstPackage == null)
                                {
                                    flvFirstPackage = realValue;
                                }
                                if (jT1078HttpSessionManager.GetAll().Count() > 0)
                                {
                                    foreach (var session in jT1078HttpSessionManager.GetAll())
                                    {
                                        if (!exists.ContainsKey(session.Channel.Id.AsShortText()))
                                        {
                                            IFullHttpResponse firstRes = new DefaultFullHttpResponse(HttpVersion.Http11, HttpResponseStatus.OK);
                                            firstRes.Headers.Set(ServerEntity, ServerName);
                                            firstRes.Headers.Set(DateEntity, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"));
                                            firstRes.Headers.Set(HttpHeaderNames.AccessControlAllowOrigin, "*");
                                            firstRes.Headers.Set(HttpHeaderNames.AccessControlAllowMethods, "GET,POST,HEAD,PUT,DELETE,OPTIONS");
                                            firstRes.Headers.Set(HttpHeaderNames.AccessControlAllowCredentials, "*");
                                            firstRes.Headers.Set(HttpHeaderNames.AccessControlAllowHeaders, "origin,range,accept-encoding,referer,Cache-Control,X-Proxy-Authorization,X-Requested-With,Content-Type");
                                            firstRes.Headers.Set(HttpHeaderNames.AccessControlExposeHeaders, "Server,range,Content-Length,Content-Range");
                                            firstRes.Headers.Set(HttpHeaderNames.AcceptRanges, "bytes");
                                            firstRes.Headers.Set(HttpHeaderNames.ContentType, "video/x-flv");
                                            firstRes.Headers.Set(HttpHeaderNames.Connection, "Keep-Alive");
                                            //HttpUtil.SetContentLength(firstRes, long.MaxValue);
                                            firstRes.Content.WriteBytes(flvFirstPackage);
                                            session.Channel.WriteAndFlushAsync(firstRes);
                                            exists.TryAdd(session.Channel.Id.AsShortText(), 0);
                                        }
                                        IFullHttpResponse res2 = new DefaultFullHttpResponse(HttpVersion.Http11, HttpResponseStatus.OK);
                                        res2.Headers.Set(ServerEntity, ServerName);
                                        res2.Headers.Set(DateEntity, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"));
                                        res2.Headers.Set(HttpHeaderNames.AccessControlAllowOrigin, "*");
                                        res2.Headers.Set(HttpHeaderNames.AccessControlAllowMethods, "GET,POST,HEAD,PUT,DELETE,OPTIONS");
                                        res2.Headers.Set(HttpHeaderNames.AccessControlAllowCredentials, "*");
                                        res2.Headers.Set(HttpHeaderNames.AccessControlAllowHeaders, "origin,range,accept-encoding,referer,Cache-Control,X-Proxy-Authorization,X-Requested-With,Content-Type");
                                        res2.Headers.Set(HttpHeaderNames.AccessControlExposeHeaders, "Server,range,Content-Length,Content-Range");
                                        res2.Headers.Set(HttpHeaderNames.AcceptRanges, "bytes");
                                        res2.Headers.Set(HttpHeaderNames.ContentType, "video/x-flv");
                                        res2.Headers.Set(HttpHeaderNames.Connection, "Keep-Alive");
                                        //HttpUtil.SetContentLength(res2, long.MaxValue);
                                        res2.Content.WriteBytes(realValue);
                                        session.Channel.WriteAndFlushAsync(res2);
                                    }
                                }
                                //Console.WriteLine(JsonConvert.SerializeObject(realValue)+"-"+ length.ToString());
                            }
                        }
                        else
                        {
                            if (!pipeServerOut.IsConnected)
                            {
                                Console.WriteLine("WaitForConnection Star...");
                                pipeServerOut.WaitForConnectionAsync();
                                Console.WriteLine("WaitForConnection End...");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex);
                    }
                }
            });
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            try
            {
                process.Kill();
                pipeServerOut.Flush();
                pipeServerOut.Close();
            }
            catch
            {


            }
            return Task.CompletedTask;
        }
    }
}
