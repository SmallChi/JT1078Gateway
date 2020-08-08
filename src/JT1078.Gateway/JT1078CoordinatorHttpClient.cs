using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;

namespace JT1078.Gateway
{
    /// <summary>
    /// 协调器客户端
    /// </summary>
    public class JT1078CoordinatorHttpClient
    {
        private HttpClient httpClient;

        public JT1078CoordinatorHttpClient(HttpClient httpClient)
        {
            this.httpClient = httpClient;
        }

        /// <summary>
        /// 发送心跳至协调器中
        /// </summary>
        /// <param name="content"></param>
        public async void Heartbeat(string content)
        {
            await httpClient.PostAsync("/heartbeat", new StringContent(content));
        }
    }
}
