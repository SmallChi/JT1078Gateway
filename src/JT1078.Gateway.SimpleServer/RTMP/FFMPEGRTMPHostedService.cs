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

namespace JT1078.Gateway.SimpleServer.RTMP
{
    /// <summary>
    /// 1.部署 RTMP 服务器 https://github.com/a1q123456/Harmonic
    /// 2.使用ffplay播放器查看 ./ffplay rtmp://127.0.0.1/living/streamName
    /// ref:
    /// https://stackoverflow.com/questions/32157774/ffmpeg-output-pipeing-to-named-windows-pipe
    /// https://mathewsachin.github.io/blog/2017/07/28/ffmpeg-pipe-csharp.html
    /// https://csharp.hotexamples.com/examples/-/NamedPipeServerStream/-/php-namedpipeserverstream-class-examples.html
    /// 
    /// ffmpeg pipe作为客户端 
    /// NamedPipeServerStream作为服务端
    /// </summary>
    class FFMPEGRTMPHostedService : IHostedService
    {
        private readonly Process process;
        public FFMPEGRTMPHostedService()
        {
            process = new Process
            {
                StartInfo =
                {
                    FileName = @"C:\ffmpeg\bin\ffmpeg.exe",
                    Arguments = $@"-f dshow -i video={HardwareCamera.CameraName} -c copy -f flv -vcodec h264 {HardwareCamera.RTMPURL}",
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            process.Start();
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            process.Kill();
            return Task.CompletedTask;
        }
    }
}
