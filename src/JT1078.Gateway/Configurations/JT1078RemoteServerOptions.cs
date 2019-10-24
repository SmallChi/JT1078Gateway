using Microsoft.Extensions.Options;
using System.Collections.Generic;

namespace JT1078.Gateway.Configurations
{
    public class JT1078RemoteServerOptions:IOptions<JT1078RemoteServerOptions>
    {
        public List<string> RemoteServers { get; set; }

        public JT1078RemoteServerOptions Value => this;
    }
}
