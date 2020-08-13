using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using JT1078.Gateway.Coordinator.Dtos;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace JT1078.Gateway.Coordinator.Controller
{
    /// <summary>
    /// 协调器中心
    /// </summary>
    [Route("JT1078WebApi/Coordinator")]
    [ApiController]
    [EnableCors("any")]
    public class CoordinatorController:ControllerBase
    {
        private ILogger logger;
        public CoordinatorController(ILoggerFactory loggerFactory)
        {
            logger = loggerFactory.CreateLogger<CoordinatorController>();
        }

        /// <summary>
        /// 集群服务器重置
        /// </summary>
        [Route("Reset")]
        [HttpPost]
        public void Reset()
        {

        }

        /// <summary>
        /// 心跳检测
        /// </summary>
        [Route("Heartbeat")]
        [HttpPost]
        public void Heartbeat([FromBody] HeartbeatRequest request)
        {
            
        }      
        
        /// <summary>
        /// 关闭通道
        /// </summary>
        [Route("ChannelClose")]
        [HttpPost]
        public void ChannelClose([FromBody] ChannelCloseRequest request)
        {

        }
    }
}
