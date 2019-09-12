using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Text;

namespace JT1078.DotNetty.TestHosting
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
    public class FFMPEGRTMPPipingService : IDisposable
    {
        private readonly Process process;
        private readonly NamedPipeServerStream pipeServer;
        public FFMPEGRTMPPipingService(string pipeName)
        {
            pipeServer = new NamedPipeServerStream(pipeName, PipeDirection.Out, 1, PipeTransmissionMode.Byte, PipeOptions.WriteThrough, 10000, 10000);
            string rtmp = "rtmp://127.0.0.1/living/streamName";
            process = new Process
            {
                StartInfo =
                {
                    FileName = @"C:\ffmpeg\bin\ffmpeg.exe",
                    Arguments = $@"-i \\.\pipe\{pipeName} -c copy -f flv {rtmp}",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardError = true
                }
            };
            process.Start();
            process.ErrorDataReceived += ErrorDataReceived;
            pipeServer.WaitForConnection();
        }

        public void ErrorDataReceived(object sender, DataReceivedEventArgs e)
        {
            Console.WriteLine(e.Data);
        }

        public void Wirte(byte[] buffer)
        {
            if (pipeServer.IsConnected)
                pipeServer.WriteAsync(buffer);
        }

        public void Dispose()
        {
            try
            {
                process.Kill();
                pipeServer.Flush();
            }
            catch 
            {

            }
            process.Dispose();
            pipeServer.Dispose();
        }
    }
}
