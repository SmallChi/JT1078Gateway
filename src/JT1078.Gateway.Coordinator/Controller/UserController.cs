using JT1078.Gateway.Coordinator.Dtos;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace JT1078.Gateway.Coordinator.Controller
{
    /// <summary>
    /// 用户功能
    /// </summary>
    [Route("JT1078WebApi/User")]
    [ApiController]
    [EnableCors("any")]
    public class UserController : ControllerBase
    {
        /// <summary>
        /// 登录
        /// </summary>
        [Route("Login")]
        [HttpPost]
        public void Login([FromBody] LoginRequest request)
        {

        }
    }
}
