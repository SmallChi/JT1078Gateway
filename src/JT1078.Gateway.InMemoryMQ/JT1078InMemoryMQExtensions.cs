using JT1078.Gateway.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System;
using System.Collections.Generic;
using System.Text;

namespace JT1078.Gateway.InMemoryMQ
{
    public static class JT1078InMemoryMQExtensions
    {
        public static IJT1078NormalGatewayBuilder AddMsgProducer(this IJT1078NormalGatewayBuilder builder)
        {
            builder.JT1078Builder.Services.TryAddSingleton<JT1078MsgChannel>();
            builder.JT1078Builder.Services.AddSingleton<IJT1078PackageProducer, JT1078PackageProducer>();
            return builder;
        }

        public static IJT1078NormalGatewayBuilder AddMsgConsumer(this IJT1078NormalGatewayBuilder builder)
        {
            builder.JT1078Builder.Services.TryAddSingleton<JT1078MsgChannel>();
            builder.JT1078Builder.Services.AddSingleton<IJT1078PackageConsumer, JT1078PackageConsumer>();
            return builder;
        }
    }
}
