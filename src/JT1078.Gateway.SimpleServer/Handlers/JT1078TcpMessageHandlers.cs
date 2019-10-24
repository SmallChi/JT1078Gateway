using DotNetty.Buffers;
using JT1078.Gateway.Interfaces;
using JT1078.Gateway.Metadata;
using JT1078.Gateway.SimpleServer.JT1078WSFlv;
using JT1078.Protocol;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace JT1078.Gateway.SimpleServer.Handlers
{
    public class JT1078TcpMessageHandlers : IJT1078TcpMessageHandlers
    {
        private readonly ILogger logger;
        private readonly ILogger hexLogger;
        private readonly JT1078WSFlvDataService jT1078WSFlvDataService;
        public JT1078TcpMessageHandlers(
             JT1078WSFlvDataService jT1078WSFlvDataServic,
            ILoggerFactory loggerFactory)
        {
            this.jT1078WSFlvDataService = jT1078WSFlvDataServic;
            logger = loggerFactory.CreateLogger("JT1078TcpMessageHandlers");
            hexLogger = loggerFactory.CreateLogger("JT1078TcpMessageHandlersHex");
            //var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "h264", "demo.h264");
            //if (!File.Exists(path))
            //{
            //    File.Create(path);
            //}
        }

        public Task<JT1078Response> Processor(JT1078Request request)
        {
            //var path=Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "h264", $"demo.h264");
            //using (FileStream fs = new FileStream(path, FileMode.Append, FileAccess.Write))
            //{
            //    fs.Write(request.Src);
            //    fs.Close();
            //}
            logger.LogInformation(JsonConvert.SerializeObject(request.Package));
            //hexLogger.LogInformation($"{request.Package.SIM},{request.Package.Label3.DataType.ToString()},{request.Package.LastFrameInterval},{request.Package.LastIFrameInterval},{request.Package.Timestamp},{request.Package.SN},{request.Package.LogicChannelNumber},{request.Package.Label3.SubpackageType.ToString()},{ByteBufferUtil.HexDump(request.Src)}");
            hexLogger.LogInformation($"{request.Package.SIM},{request.Package.SN},{request.Package.LogicChannelNumber},{request.Package.Label3.DataType.ToString()},{request.Package.Label3.SubpackageType.ToString()},{ByteBufferUtil.HexDump(request.Src)}");
            var mergePackage = JT1078Serializer.Merge(request.Package);
            if (mergePackage != null)
            {
                jT1078WSFlvDataService.JT1078Packages.Add(mergePackage);
            }
            return Task.FromResult<JT1078Response>(default);
        }
    }
}
