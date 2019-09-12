using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Text;
using System.Threading.Tasks;

namespace JT1078.DotNetty.TestHosting
{

    public class FFMPEGHTTPFLVPipingService : IDisposable
    {
        private readonly Process process;
        private readonly NamedPipeServerStream pipeServer;
        private readonly NamedPipeServerStream pipeServerOut;
        public FFMPEGHTTPFLVPipingService(string pipeName)
        {
            pipeServer = new NamedPipeServerStream(pipeName, PipeDirection.Out, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous);
            pipeServerOut = new NamedPipeServerStream(pipeName+ "out", PipeDirection.In, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous);
            process = new Process
            {
                StartInfo =
                {
                    FileName = @"C:\ffmpeg\bin\ffmpeg.exe",
                    Arguments = $@"-i \\.\pipe\{pipeName} -c copy -f flv -y \\.\pipe\{pipeName}out",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardError = true
                }
            };
            process.Start();
            process.ErrorDataReceived += ErrorDataReceived;
            pipeServer.WaitForConnection();
            Task.Run(() =>
            {
                while (true)
                {
                    try
                    {
                        if (pipeServerOut.IsConnected)
                        {
                            if (pipeServerOut.CanRead)
                            {
                                var value = pipeServerOut.ReadByte();
                                Console.WriteLine(value);
                            }
                        }
                        else
                        {
                            pipeServerOut.WaitForConnectionAsync();
                        }

                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex);
                    }
                    
                }
            });
        }

        public void ErrorDataReceived(object sender, DataReceivedEventArgs e)
        {
            Console.WriteLine(e.Data);
        }
        public void OutputDataReceived(object sender, DataReceivedEventArgs e)
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
                process.WaitForExit();
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
