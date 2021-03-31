using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace JT1078.Gateway
{
    /// <summary>
    /// hls路径是否存在处理，及文件监控处理
    /// </summary>
    public class HLSPathStorage
    {
        private readonly ConcurrentDictionary<string, string> path_sim_channelDic = new ConcurrentDictionary<string, string>();
        private readonly ConcurrentDictionary<string, FileSystemWatcher> pathFileSystemWaterDic = new ConcurrentDictionary<string, FileSystemWatcher>();
        /// <summary>
        /// 添加路径
        /// </summary>
        /// <param name="path"></param>
        public void AddPath(string path,string key) {
            path_sim_channelDic.TryAdd(path, key);
        }
        /// <summary>
        /// 判断路径是否存在
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public bool ExsitPath(string path) {
            return path_sim_channelDic.TryGetValue(path, out var _);
        }
        /// <summary>
        /// 移除所有路径
        /// </summary>
        /// <returns></returns>
        public bool RemoveAllPath(string key) {
            var flag = false;
            var paths = path_sim_channelDic.Where(m => m.Value == key).ToList();
            foreach (var item in paths)
            {
                flag = path_sim_channelDic.TryRemove(item.Key,out var _);
            }
            return flag;
        }

        /// <summary>
        /// 是否存在文件监控
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public bool ExistFileSystemWatcher(string path) {
            return pathFileSystemWaterDic.TryGetValue(path, out var _);
        }
        /// <summary>
        /// 添加文件监控
        /// </summary>
        /// <param name="path"></param>
        /// <param name="fileSystemWatcher"></param>
        public void AddFileSystemWatcher(string path, FileSystemWatcher fileSystemWatcher) {
            pathFileSystemWaterDic.TryAdd(path, fileSystemWatcher);
        }
        /// <summary>
        /// 删除文件监控
        /// </summary>
        /// <param name="path"></param>
        public bool DeleteFileSystemWatcher(string path)
        {
           return  pathFileSystemWaterDic.TryRemove(path, out var _);
        }
    }
}
