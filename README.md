# JT1078Gateway

## 前提条件

1. [熟悉JT1078协议](https://github.com/SmallChi/JT1078)
2. 了解Http Chunked编码
3. 了解WebSocket消息推送
4. [了解flv.js](https://github.com/bilibili/flv.js)
5. [了解hls.js](https://github.com/video-dev/hls.js)
6. 了解fmp4

> 注意：暂不支持音频

| av | video | audio |test|request|
| --- | ---| --- |---|---|
| flv  | 😀| ☹ |😀|http-flv、ws-flv|
| m3u8 | 😀| ☹ |😀|http|
| fmp4 | 😀| ☹ |😀(部分设备可用)|http-fmp4[X]、ws-fmp4[✔]|

## NuGet安装

| Package Name  | Version |Pre Version|Downloads|
| --- | ---| --- | --- |
| Install-Package JT1078.Gateway.Abstractions | ![JT1078.Gateway.Abstractions](https://img.shields.io/nuget/v/JT1078.Gateway.Abstractions.svg) | ![JT1078.Gateway.Abstractions](https://img.shields.io/nuget/vpre/JT1078.Gateway.Abstractions.svg) | ![JT1078.Gateway.Abstractions](https://img.shields.io/nuget/dt/JT1078.Gateway.Abstractions.svg) |
| Install-Package JT1078.Gateway | ![JT1078.Gateway](https://img.shields.io/nuget/v/JT1078.Gateway.svg) | ![JT1078.Gateway](https://img.shields.io/nuget/vpre/JT1078.Gateway.svg)|![JT1078.Gateway](https://img.shields.io/nuget/dt/JT1078.Gateway.svg) |
| Install-Package JT1078.Gateway.InMemoryMQ | ![JT1078.Gateway.InMemoryMQ](https://img.shields.io/nuget/v/JT1078.Gateway.InMemoryMQ.svg) |  ![JT1078.Gateway.InMemoryMQ](https://img.shields.io/nuget/vpre/JT1078.Gateway.InMemoryMQ.svg) | ![JT1078.Gateway.InMemoryMQ](https://img.shields.io/nuget/dt/JT1078.Gateway.InMemoryMQ.svg) |
