using JT1078.Gateway.Abstractions;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace JT1078.Gateway.TestNormalHosting.Services
{
    public class JT1078NormalMsgHostedService : BackgroundService
    {
        private IJT1078PackageConsumer PackageConsumer;
        public JT1078NormalMsgHostedService(IJT1078PackageConsumer packageConsumer)
        {
            PackageConsumer = packageConsumer;
        }
        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            PackageConsumer.OnMessage((Message) => 
            {

            });
            return Task.CompletedTask;
        }
    }
}
