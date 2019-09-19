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
using Microsoft.AspNetCore.Hosting;
using System.Net;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Cors;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Builder;
using JT1078.DotNetty.TestHosting.HLS;
using Microsoft.Extensions.Logging;

namespace JT1078.DotNetty.TestHosting
{
    /// <summary>
    /// 
    /// -segment_time 5秒切片
    /// ./ffmpeg -f dshow -i video="USB2.0 PC CAMERA" -start_number 0 -hls_list_size 0 -f hls "D:\v\sample.m3u8 -segment_time 5"
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
                    Arguments = $@"-f dshow -i video={HardwareCamera.CameraName} -vcodec h264 -start_number 0 -hls_list_size 10 -f hls {filePath}",
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
            process.Kill();
            webHost.WaitForShutdownAsync();
            return Task.CompletedTask;
        }
    }
}
