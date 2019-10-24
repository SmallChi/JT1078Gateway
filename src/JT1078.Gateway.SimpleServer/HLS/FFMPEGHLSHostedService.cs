using DotNetty.Buffers;
using DotNetty.Codecs.Http.WebSockets;
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
using Microsoft.AspNetCore.Hosting;
using System.Net;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Cors;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Logging;

namespace JT1078.Gateway.SimpleServer.HLS
{
    /// <summary>
    /// 
    /// -hls_list_size 10  m3u8内部文件内部保留10个集合
    /// -segment_time 10秒切片
    /// -hls_wrap 可以让切片文件进行循环 就不会导致产生很多文件了 占用很多空间
    /// ./ffmpeg -f dshow -i video="USB2.0 PC CAMERA" -hls_wrap 20 -start_number 0 -hls_list_size 10 -f hls "D:\v\sample.m3u8 -segment_time 10"
    /// </summary>
    class FFMPEGHLSHostedService : IHostedService
    {
        private readonly Process process;
        private const string FileName= "hls_ch1.m3u8";
        private const string DirectoryName = "hlsvideo";
        private readonly IWebHost webHost;
        public FFMPEGHLSHostedService()
        {
            string directoryPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, DirectoryName);
            if (Directory.Exists(directoryPath))
            {
                Directory.Delete(directoryPath,true);
                Directory.CreateDirectory(directoryPath);
            }
            else
            {
                Directory.CreateDirectory(directoryPath);
            }
            string filePath =$"\"{Path.Combine(directoryPath, FileName)}\"";
            process = new Process
            {
                StartInfo =
                {
                    FileName = @"C:\ffmpeg\bin\ffmpeg.exe",
                    Arguments = $@"-f dshow -i video={HardwareCamera.CameraName} -vcodec h264 -hls_wrap 10 -start_number 0 -hls_list_size 10 -f hls {filePath} -segment_time 10",
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };
            webHost= new WebHostBuilder()
                .ConfigureLogging((_, factory) =>
                {
                    factory.SetMinimumLevel(LogLevel.Debug);
                    factory.AddConsole();
                })
                .ConfigureAppConfiguration((hostingContext, config) =>
                {
                    config.SetBasePath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"HLS"));
                    config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
                })
                .UseKestrel(ksOptions =>
                {
                    ksOptions.ListenAnyIP(5001);
                })
                .UseWebRoot(AppDomain.CurrentDomain.BaseDirectory)
                .UseStartup<Startup>()
                .Build();
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            process.Start();
            webHost.RunAsync(cancellationToken);
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            try
            {
                process.Kill();
            }
            catch
            {
            }
            webHost.WaitForShutdownAsync();
            return Task.CompletedTask;
        }
    }
}
