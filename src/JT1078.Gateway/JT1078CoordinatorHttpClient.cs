using JT1078.Gateway.Configurations;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace JT1078.Gateway
{
    /// <summary>
    /// 协调器客户端
    /// </summary>
    public class JT1078CoordinatorHttpClient
    {
        private HttpClient httpClient;

        private JT1078Configuration Configuration;

        private const string endpoint = "/JT1078WebApi";

        public JT1078CoordinatorHttpClient(IOptions<JT1078Configuration> configurationAccessor)
        {
            Configuration = configurationAccessor.Value;
            this.httpClient = new HttpClient();
            this.httpClient.BaseAddress = new Uri(Configuration.CoordinatorUri);
            this.httpClient.Timeout = TimeSpan.FromSeconds(3);
        }

        /// <summary>
        /// 发送重制至协调器中
        /// </summary>
        public async ValueTask Reset()
        {
            await httpClient.GetAsync($"{endpoint}/reset");
        }

        /// <summary>
        /// 发送心跳至协调器中
        /// </summary>
        /// <param name="content"></param>
        public async ValueTask Heartbeat(string content)
        {
            await httpClient.PostAsync($"{endpoint}/heartbeat", new StringContent(content));
        }
    }
}
