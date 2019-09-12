using JT1078.Protocol;
using JT1078.Protocol.Enums;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace JT1078.DotNetty.TestHosting
{
    public class JT1078DataService
    {
        public BlockingCollection<byte[]> DataBlockingCollection { get; }

        private readonly ConcurrentDictionary<string, byte[]> SubcontractKey;
        public JT1078DataService()
        {
            DataBlockingCollection = new BlockingCollection<byte[]>(60000);
            SubcontractKey = new ConcurrentDictionary<string, byte[]>(StringComparer.OrdinalIgnoreCase);
            var lines = File.ReadAllLines(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "2019-08-27-1.log"));
            Task.Run(() =>
            {
                while (true)
                {
                    for (int i = 0; i < lines.Length; i++)
                    {
                        try
                        {
                            var item = JT1078Serializer.Deserialize(lines[i].Split(',')[6].ToHexBytes());
                            if (item.Label3.SubpackageType == JT1078SubPackageType.分包处理时的第一个包)
                            {
                                SubcontractKey.TryRemove(item.SIM, out _);
                                SubcontractKey.TryAdd(item.SIM, item.Bodies);
                            }
                            else if (item.Label3.SubpackageType == JT1078SubPackageType.分包处理时的中间包)
                            {
                                if (SubcontractKey.TryGetValue(item.SIM, out var buffer))
                                {
                                    SubcontractKey[item.SIM] = buffer.Concat(item.Bodies).ToArray();
                                }
                            }
                            else if (item.Label3.SubpackageType == JT1078SubPackageType.分包处理时的最后一个包)
                            {
                                if (SubcontractKey.TryGetValue(item.SIM, out var buffer))
                                {
                                    DataBlockingCollection.Add(buffer.Concat(item.Bodies).ToArray());
                                }
                            }
                            else
                            {
                                DataBlockingCollection.Add(item.Bodies);
                            }
                        }
                        catch 
                        {

                        }
                    }
                    Thread.Sleep(60000);
                }
            });
        }
    }
}
