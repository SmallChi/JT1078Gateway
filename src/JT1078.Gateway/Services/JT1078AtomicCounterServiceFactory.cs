using JT1078.Gateway.Enums;
using System;
using System.Collections.Concurrent;

namespace JT1078.Gateway.Session.Services
{
    public class JT1078AtomicCounterServiceFactory
    {
        private readonly ConcurrentDictionary<JT1078TransportProtocolType, JT1078AtomicCounterService> cache;

        public JT1078AtomicCounterServiceFactory()
        {
            cache = new ConcurrentDictionary<JT1078TransportProtocolType, JT1078AtomicCounterService>();
        }

        public JT1078AtomicCounterService Create(JT1078TransportProtocolType type)
        {
            if(cache.TryGetValue(type,out var service))
            {
                return service;
            }
            else
            {
                var serviceNew = new JT1078AtomicCounterService();
                cache.TryAdd(type, serviceNew);
                return serviceNew;
            }
        }
    }
}
