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
        public static IJT1078GatewayBuilder AddMsgProducer(this IJT1078GatewayBuilder builder)
        {
            builder.JT1078Builder.Services.TryAddSingleton<JT1078MsgChannel>();
            builder.JT1078Builder.Services.AddSingleton<IJT1078MsgProducer, JT1078MsgProducer>();
            return builder;
        }

        public static IJT1078GatewayBuilder AddMsgConsumer(this IJT1078GatewayBuilder builder)
        {
            builder.JT1078Builder.Services.TryAddSingleton<JT1078MsgChannel>();
            builder.JT1078Builder.Services.AddSingleton<IJT1078MsgConsumer, JT1078MsgConsumer>();
            return builder;
        }
    }
}
