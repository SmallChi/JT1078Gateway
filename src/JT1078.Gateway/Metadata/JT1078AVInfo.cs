using System;
using System.Collections.Generic;
using System.Text;

namespace JT1078.Gateway.Metadata
{
    /// <summary>
    /// 音视频信息
    /// </summary>
    public struct JT1078AVInfo
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="sim"></param>
        /// <param name="channelNo"></param>
        public JT1078AVInfo(string sim, int channelNo)
        {
            Sim = sim;
            ChannelNo = channelNo;
        }
        /// <summary>
        /// sim
        /// </summary>
        public string Sim { get; set; }
        /// <summary>
        /// 通道号
        /// </summary>
        public int ChannelNo { get; set; }
        /// <summary>
        /// key
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return $"{Sim}_{ChannelNo}";
        }
    }
}
