# JT1078DotNetty

## 前提条件

1. [熟悉JT1078协议](https://github.com/SmallChi/JT1078)
2. 了解Http Chunked编码
3. 了解WebSocket消息推送
4. [了解Flv.js](https://github.com/bilibili/flv.js)

目前只支持Http-Flv、WebSocket-Flv两种方式推流，经过一小时的测试延迟在3秒这样。

## NuGet安装

| Package Name          | Version                                            | Downloads                                           |
| --------------------- | -------------------------------------------------- | --------------------------------------------------- |
| Install-Package JT1078.DotNetty.Core | ![JT1078.DotNetty.Core](https://img.shields.io/nuget/v/JT1078.DotNetty.Core.svg) | ![JT1078.DotNetty.Core](https://img.shields.io/nuget/dt/JT1078.DotNetty.Core.svg) |
| Install-Package JT1078.DotNetty.Tcp | ![JT1078.DotNetty.Tcp](https://img.shields.io/nuget/v/JT1078.DotNetty.Tcp.svg) | ![JT1078.DotNetty.Tcp](https://img.shields.io/nuget/dt/JT1078.DotNetty.Tcp.svg) |
| Install-Package JT1078.DotNetty.Udp | ![JT1078.DotNetty.Udp](https://img.shields.io/nuget/v/JT1078.DotNetty.Udp.svg) | ![JT1078.DotNetty.Udp](https://img.shields.io/nuget/dt/JT1078.DotNetty.Udp.svg) |
| Install-Package JT1078.DotNetty.Http | ![JT1078.DotNetty.Http](https://img.shields.io/nuget/v/JT1078.DotNetty.Http.svg) | ![JT1078.DotNetty.Http](https://img.shields.io/nuget/dt/JT1078.DotNetty.Http.svg) |