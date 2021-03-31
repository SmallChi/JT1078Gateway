using JT1078.Protocol;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace JT1078.Gateway.TestNormalHosting.Services
{
     public class MessageDispatchDataService
    {
        public Channel<JT1078Package> HlsChannel = Channel.CreateUnbounded<JT1078Package>();
        public Channel<JT1078Package> FlvChannel = Channel.CreateUnbounded<JT1078Package>();
    }
}
