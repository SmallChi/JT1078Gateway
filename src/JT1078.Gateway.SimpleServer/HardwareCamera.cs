using System;
using System.Collections.Generic;
using System.Text;

namespace JT1078.Gateway.SimpleServer
{
    /// <summary>
    /// 
    /// .\ffmpeg -list_options true -f dshow -i video = "USB2.0 PC CAMERA"
    /// .\ffmpeg -f dshow -i video = "USB2.0 PC CAMERA" -vcodec libx264 "D:\mycamera.flv"
    /// 
    /// .\ffmpeg -f dshow -i video = "USB2.0 PC CAMERA" - c copy -f flv -vcodec h264 "rtmp://127.0.0.1/living/streamName"
    /// .\ffplay rtmp://127.0.0.1/living/streamName
    /// 
    /// .\ffmpeg -f dshow -i video = "USB2.0 PC CAMERA" - c copy -f -y flv -vcodec h264 "pipe://demoserverout"
    /// 
    /// ref:https://www.cnblogs.com/lidabo/p/8662955.html
    /// </summary>
    public static class HardwareCamera
    {
        public const string CameraName = "\"USB2.0 PC CAMERA\"";
        public const string RTMPURL = "rtmp://127.0.0.1/living/streamName";
    }
}
