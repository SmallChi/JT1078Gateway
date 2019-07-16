using JT1078.Protocol;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace JT1078.DotNetty.TestHosting
{
    public class JT1078DataService
    {
        public BlockingCollection<JT1078Package> DataBlockingCollection { get; }

        public JT1078DataService()
        {
            DataBlockingCollection = new BlockingCollection<JT1078Package>(60000);
            var lines = File.ReadAllLines(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "2019-07-15.log"));
            Task.Run(() =>
            {
                while (true)
                {
                    for (int i = 0; i < lines.Length; i++)
                    {
                        try
                        {
                            var package = JT1078Serializer.Deserialize(lines[i].Split(',')[6].ToHexBytes());
                            DataBlockingCollection.Add(package);
                            Thread.Sleep(300);
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
