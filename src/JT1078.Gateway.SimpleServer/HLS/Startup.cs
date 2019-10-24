using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;

namespace JT1078.Gateway.SimpleServer.HLS
{
    public class Startup
    {
        public void Configure(IApplicationBuilder app, ILoggerFactory loggerFactory)
        {
            //mime
            //https://developer.apple.com/library/archive/documentation/NetworkingInternet/Conceptual/StreamingMediaGuide/DeployingHTTPLiveStreaming/DeployingHTTPLiveStreaming.html
            var Provider = new FileExtensionContentTypeProvider();
            Provider.Mappings[".m3u8"] = "application/x-mpegURL,vnd.apple.mpegURL";
            Provider.Mappings[".ts"] = "video/MP2T";
            app.UseStaticFiles(new StaticFileOptions()
            {
                ContentTypeProvider = Provider
            });
        }
    }
}
