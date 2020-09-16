using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace JT1078.Gateway.Coordinator.Dtos
{
    public class ChannelCloseRequest
    {
        /// <summary>
        /// 设备sim卡号
        /// </summary>
        public string Sim { get; set; }
        /// <summary>
        /// 通道号
        /// </summary>
        public int ChannelNo { get; set; }
    }
}
