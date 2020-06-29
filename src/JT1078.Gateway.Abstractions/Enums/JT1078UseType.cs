using System;
using System.Collections.Generic;
using System.Text;

namespace JT1078.Gateway.Abstractions.Enums
{
    public enum JT1078UseType : byte
    {
        /// <summary>
        /// 使用正常方式
        /// </summary>
        Normal = 1,
        /// <summary>
        /// 使用队列方式
        /// </summary>
        Queue = 2
    }
}
