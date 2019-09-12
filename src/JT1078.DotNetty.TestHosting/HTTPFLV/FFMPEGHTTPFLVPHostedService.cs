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

namespace JT1078.DotNetty.TestHosting
{
    class FFMPEGHTTPFLVPHostedService : IHostedService
    {
        private readonly JT1078DataService jT1078DataService;
        private readonly FFMPEGHTTPFLVPipingService  fFMPEGHTTPFLVPipingService;
        public FFMPEGHTTPFLVPHostedService(
            JT1078DataService jT1078DataService)
        {
            this.jT1078DataService = jT1078DataService;
            fFMPEGHTTPFLVPipingService = new FFMPEGHTTPFLVPipingService("demo2");
        }
        public Task StartAsync(CancellationToken cancellationToken)
        {
            Task.Run(() =>
            {
                try
                {
                    foreach (var item in jT1078DataService.DataBlockingCollection.GetConsumingEnumerable(cancellationToken))
                    {
                        fFMPEGHTTPFLVPipingService.Wirte(item);
                    }
                }
                catch(Exception ex)
                {
                    Console.WriteLine(ex);
                }
                
            }, cancellationToken);
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
