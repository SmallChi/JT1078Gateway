using System;
using System.Buffers;
using JT1078.Protocol.Extensions;
using System.Collections.Generic;
using System.IO.Pipelines;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using JT1078.Protocol;
using System.Buffers.Binary;
using JT1078.Protocol.Enums;
using System.Linq;
using System.Text;
using System.IO;

namespace JT1078.Gateway.Test
{
    public class PipeTest
    {
        [Fact]
        public void Test1()
        {
            var reader = new ReadOnlySequence<byte>("303163648162000000190130503701010000016f95973f840000000003b600000001674d001495a85825900000000168ee3c800000000106e5010d800000000165b80000090350bfdfe840b5b35c676079446e3ffe2f5240e25cc6b35cebac31720656a853ba1b571246fb858eaa5d8266acb95b92705fd187f1fd20ff0ca6b62c4cbcb3b662f5d61c016928ca82b411acdc4df6edb2034624b992eee9b6c241e1903bf9477c6e4293b65ba75e98d5a2566da6f71c85e1052a9d5ed35c393b1a73b181598749f3d26f6fbf48f0be61c673fcb9f2b0d305794bed03af5e3cedff7768bed3120261d6f3547a6d519943c2afcb80e423c9e6db088a06200dbfaa81edc5bc0de67957e791f67bf040ef944f7d62983d32517b2fb2d9572a71340c225617231bc0d98e66d19fe81a19b44280860b273f700bf3f3444a928e93fefc716e2af46995fbb658d0580a49e42f6835270c8c154abe28a17f76550b1b1fafe62945f80490b3f780fe9bb4d4b4107eac3d50b8c99d1a191f6754992096683fb0f599846bae759b06222079f5404be39e4416136c7c42255b0e7ca42d86fc2227892406d61f9816bc125d017989a671f13f2f4052e018b1fb02460802029a049a23d2ffeea6ac552109d35aa8731483fb2cae963987156056cafb32436a23a0dc918fb2440b14c9e6124441e7bb3b08706066d1ddab512267767b6e522f80732e67046ff5ad4d8193bf5cc5c05ccceb73a36b6c3ea39fa91bb308c8bb7bf88515d9c52409128e8b94e33e48a5396c35c20bd83b7c0e6d3d4a24bc14e84810066c686c6c04e687c41123fe87c89d5fa07b0095e7f82d3b07e72570163c47444bdde16ae9bfacd540df047e8ee34e98ff33178da5c7e6be9272e6dcfbb6db7e678a6d1d3832226c9bf85afa14feac15a270d5d3724a121b8fc9b40f0d37bb7f432de5421d286a65313a6efd251f7ed75b4ef6557975af5da5df2b87a0bbc1cb58183c4c1e24fdc4eb016777af1a6fa4a29d3eed7c4463482e591a6dc20540cabb6d7dd29cbb8ffdacafdaac2dd36db70fefe14fdeec85ef5fe01bb104d2d6439dbd7ceefc87007ce07b8409751dd7c21aa9a537f5fdefdef7d6ceba8d5ae876522f75dedd472e4dde1284e71380ee75ed313b2b9b9a94a56ebd03ae36b64a3b35abbdc7ba380016218201d156658ed9b5632f80f921879063e9037cd3509d01a2e91c17e03d892e2bc381ac723eba266497a1fbb0dc77ab3f4a9a981f95977b025b005a0e09b1add481888333927963fc5e5bf376655cb00e4ca8841fa450c8653f91cf2f3fb0247dbcace5dfde3af4a854f9fa2aaaa33706a78321332273ab4ee837ff4f8eba08676e7f889464a842b8e3e4a579d2".ToHexBytes());
            SequenceReader<byte> seqReader = new SequenceReader<byte>(reader);
            long totalConsumed = 0;
            List<byte[]> packages = new List<byte[]>();
            while (!seqReader.End)
            {
                var header = seqReader.Sequence.Slice(seqReader.Consumed, 4);
                var headerValue = BinaryPrimitives.ReadUInt32BigEndian(header.FirstSpan);
                Assert.Equal(JT1078Package.FH, headerValue);
                if(JT1078Package.FH == headerValue)
                {
                    //sim
                    var sim = ReadBCD(seqReader.Sequence.Slice(seqReader.Consumed + 8, 6).FirstSpan, 12);
                    Assert.Equal("1901305037", sim);
                    //根据数据类型处理对应的数据长度
                    seqReader.Advance(15);
                    if(seqReader.TryRead(out byte dataType))
                    {
                        JT1078Label3 label3 = new JT1078Label3(dataType);
                        Assert.Equal(JT1078DataType.视频I帧, label3.DataType);
                        int bodyLength = 0;
                        //透传的时候没有该字段
                        if (label3.DataType!= JT1078DataType.透传数据)
                        {
                            //时间戳
                            bodyLength += 8;
                        }
                        //非视频帧时没有该字段
                        if (label3.DataType == JT1078DataType.视频I帧 || 
                            label3.DataType == JT1078DataType.视频P帧 ||
                            label3.DataType == JT1078DataType.视频B帧)
                        {
                            //上一个关键帧 + 上一帧 = 2 + 2
                            bodyLength += 4;
                        }
                        seqReader.Advance(bodyLength);
                        var bodyLengthFirstSpan = seqReader.Sequence.Slice(seqReader.Consumed, 2).FirstSpan;
                        //数据体长度
                        seqReader.Advance(2);
                        bodyLength = BinaryPrimitives.ReadUInt16BigEndian(bodyLengthFirstSpan);
                        Assert.Equal(950, bodyLength);
                        //数据体
                        seqReader.Advance(bodyLength);
                        var package = seqReader.Sequence.Slice(totalConsumed, seqReader.Consumed - totalConsumed).ToArray();
                        packages.Add(package);
                        totalConsumed += (seqReader.Consumed - totalConsumed);
                        if (seqReader.End) break;
                    }
                }
            }
            Assert.Single(packages);
        }

        [Fact]
        public void Test1_1()
        {
            var reader = new ReadOnlySequence<byte>("303163648162000000190130503701010000016f95973f840000000003b600000001674d001495a85825900000000168ee3c800000000106e5010d800000000165b80000090350bfdfe840b5b35c676079446e3ffe2f5240e25cc6b35cebac31720656a853ba1b571246fb858eaa5d8266acb95b92705fd187f1fd20ff0ca6b62c4cbcb3b662f5d61c016928ca82b411acdc4df6edb2034624b992eee9b6c241e1903bf9477c6e4293b65ba75e98d5a2566da6f71c85e1052a9d5ed35c393b1a73b181598749f3d26f6fbf48f0be61c673fcb9f2b0d305794bed03af5e3cedff7768bed3120261d6f3547a6d519943c2afcb80e423c9e6db088a06200dbfaa81edc5bc0de67957e791f67bf040ef944f7d62983d32517b2fb2d9572a71340c225617231bc0d98e66d19fe81a19b44280860b273f700bf3f3444a928e93fefc716e2af46995fbb658d0580a49e42f6835270c8c154abe28a17f76550b1b1fafe62945f80490b3f780fe9bb4d4b4107eac3d50b8c99d1a191f6754992096683fb0f599846bae759b06222079f5404be39e4416136c7c42255b0e7ca42d86fc2227892406d61f9816bc125d017989a671f13f2f4052e018b1fb02460802029a049a23d2ffeea6ac552109d35aa8731483fb2cae963987156056cafb32436a23a0dc918fb2440b14c9e6124441e7bb3b08706066d1ddab512267767b6e522f80732e67046ff5ad4d8193bf5cc5c05ccceb73a36b6c3ea39fa91bb308c8bb7bf88515d9c52409128e8b94e33e48a5396c35c20bd83b7c0e6d3d4a24bc14e84810066c686c6c04e687c41123fe87c89d5fa07b0095e7f82d3b07e72570163c47444bdde16ae9bfacd540df047e8ee34e98ff33178da5c7e6be9272e6dcfbb6db7e678a6d1d3832226c9bf85afa14feac15a270d5d3724a121b8fc9b40f0d37bb7f432de5421d286a65313a6efd251f7ed75b4ef6557975af5da5df2b87a0bbc1cb58183c4c1e24fdc4eb016777af1a6fa4a29d3eed7c4463482e591a6dc20540cabb6d7dd29cbb8ffdacafdaac2dd36db70fefe14fdeec85ef5fe01bb104d2d6439dbd7ceefc87007ce07b8409751dd7c21aa9a537f5fdefdef7d6ceba8d5ae876522f75dedd472e4dde1284e71380ee75ed313b2b9b9a94a56ebd03ae36b64a3b35abbdc7ba380016218201d156658ed9b5632f80f921879063e9037cd3509d01a2e91c17e03d892e2bc381ac723eba266497a1fbb0dc77ab3f4a9a981f95977b025b005a0e09b1add481888333927963fc5e5bf376655cb00e4ca8841fa450c8653f91cf2f3fb0247dbcace5dfde3af4a854f9fa2aaaa33706a78321332273ab4ee837ff4f8eba08676e7f889464a842b8e3e4a579d2".ToHexBytes());
            SequenceReader<byte> seqReader = new SequenceReader<byte>(reader);
            long totalConsumed = 0;
            List<byte[]> packages = new List<byte[]>();
            FixedHeaderInfo fixedHeaderInfo = new FixedHeaderInfo();
            while (!seqReader.End)
            {
                if (!fixedHeaderInfo.FoundHeader)
                {
                    var header = seqReader.Sequence.Slice(seqReader.Consumed, 4);
                    var headerValue = BinaryPrimitives.ReadUInt32BigEndian(header.FirstSpan);
                    Assert.Equal(JT1078Package.FH, headerValue);
                    if (JT1078Package.FH == headerValue)
                    {
                        //sim
                        var sim = ReadBCD(seqReader.Sequence.Slice(seqReader.Consumed + 8, 6).FirstSpan, 12);
                        Assert.Equal("1901305037", sim);
                        //根据数据类型处理对应的数据长度
                        fixedHeaderInfo.TotalSize += 15;
                        var dataType = seqReader.Sequence.Slice(seqReader.Consumed+fixedHeaderInfo.TotalSize, 1).FirstSpan[0];
                        fixedHeaderInfo.TotalSize += 1;
                        JT1078Label3 label3 = new JT1078Label3(dataType);
                        Assert.Equal(JT1078DataType.视频I帧, label3.DataType);
                        int bodyLength = 0;
                        //透传的时候没有该字段
                        if (label3.DataType != JT1078DataType.透传数据)
                        {
                            //时间戳
                            bodyLength += 8;
                        }
                        //非视频帧时没有该字段
                        if (label3.DataType == JT1078DataType.视频I帧 ||
                            label3.DataType == JT1078DataType.视频P帧 ||
                            label3.DataType == JT1078DataType.视频B帧)
                        {
                            //上一个关键帧 + 上一帧 = 2 + 2
                            bodyLength += 4;
                        }
                        fixedHeaderInfo.TotalSize += bodyLength;
                        var bodyLengthFirstSpan = seqReader.Sequence.Slice(seqReader.Consumed+ fixedHeaderInfo.TotalSize, 2).FirstSpan;
                        //数据体长度
                        fixedHeaderInfo.TotalSize += 2;
                        bodyLength = BinaryPrimitives.ReadUInt16BigEndian(bodyLengthFirstSpan);
                        Assert.Equal(950, bodyLength);
                        //数据体
                        fixedHeaderInfo.TotalSize += bodyLength;
                        fixedHeaderInfo.FoundHeader = true;
                    }
                }
                if (reader.Length < fixedHeaderInfo.TotalSize) break;
                seqReader.Advance(fixedHeaderInfo.TotalSize);
                var package = seqReader.Sequence.Slice(totalConsumed, seqReader.Consumed - totalConsumed).ToArray();
                packages.Add(package);
                totalConsumed += (seqReader.Consumed - totalConsumed);
                if (seqReader.End) break;
            }
            Assert.Single(packages);
        }

        [Fact]
        public void Test1_2()
        {
            var reader = new ReadOnlySequence<byte>("303163648162000000190130503701010000016f95973f84000000000000".ToHexBytes());
            SequenceReader<byte> seqReader = new SequenceReader<byte>(reader);
            long totalConsumed = 0;
            List<byte[]> packages = new List<byte[]>();
            FixedHeaderInfo fixedHeaderInfo = new FixedHeaderInfo();
            while (!seqReader.End)
            {
                if (!fixedHeaderInfo.FoundHeader)
                {
                    var header = seqReader.Sequence.Slice(seqReader.Consumed, 4);
                    var headerValue = BinaryPrimitives.ReadUInt32BigEndian(header.FirstSpan);
                    Assert.Equal(JT1078Package.FH, headerValue);
                    if (JT1078Package.FH == headerValue)
                    {
                        //sim
                        var sim = ReadBCD(seqReader.Sequence.Slice(seqReader.Consumed + 8, 6).FirstSpan, 12);
                        Assert.Equal("1901305037", sim);
                        //根据数据类型处理对应的数据长度
                        fixedHeaderInfo.TotalSize += 15;
                        var dataType = seqReader.Sequence.Slice(seqReader.Consumed + fixedHeaderInfo.TotalSize, 1).FirstSpan[0];
                        fixedHeaderInfo.TotalSize += 1;
                        JT1078Label3 label3 = new JT1078Label3(dataType);
                        Assert.Equal(JT1078DataType.视频I帧, label3.DataType);
                        int bodyLength = 0;
                        //透传的时候没有该字段
                        if (label3.DataType != JT1078DataType.透传数据)
                        {
                            //时间戳
                            bodyLength += 8;
                        }
                        //非视频帧时没有该字段
                        if (label3.DataType == JT1078DataType.视频I帧 ||
                            label3.DataType == JT1078DataType.视频P帧 ||
                            label3.DataType == JT1078DataType.视频B帧)
                        {
                            //上一个关键帧 + 上一帧 = 2 + 2
                            bodyLength += 4;
                        }
                        fixedHeaderInfo.TotalSize += bodyLength;
                        var bodyLengthFirstSpan = seqReader.Sequence.Slice(seqReader.Consumed + fixedHeaderInfo.TotalSize, 2).FirstSpan;
                        //数据体长度
                        fixedHeaderInfo.TotalSize += 2;
                        bodyLength = BinaryPrimitives.ReadUInt16BigEndian(bodyLengthFirstSpan);
                        Assert.Equal(0, bodyLength);
                        if (bodyLength == 0)
                        {
                            try
                            {
                                seqReader.Advance(fixedHeaderInfo.TotalSize);
                                var package1 = seqReader.Sequence.Slice(totalConsumed, seqReader.Consumed - totalConsumed).ToArray();
                                packages.Add(package1);
                            }
                            finally
                            {
                                totalConsumed += (seqReader.Consumed - totalConsumed);
                            }
                            continue;
                        }
                        //数据体
                        fixedHeaderInfo.TotalSize += bodyLength;
                        fixedHeaderInfo.FoundHeader = true;
                    }
                }
                if (seqReader.End) break;
            }
            Assert.Single(packages);
        }

        [Fact]
        public void Test2()
        {
            var data1 = "303163648162000000190130503701010000016f95973f840000000003b600000001674d001495a85825900000000168ee3c800000000106e5010d800000000165b80000090350bfdfe840b5b35c676079446e3ffe2f5240e25cc6b35cebac31720656a853ba1b571246fb858eaa5d8266acb95b92705fd187f1fd20ff0ca6b62c4cbcb3b662f5d61c016928ca82b411acdc4df6edb2034624b992eee9b6c241e1903bf9477c6e4293b65ba75e98d5a2566da6f71c85e1052a9d5ed35c393b1a73b181598749f3d26f6fbf48f0be61c673fcb9f2b0d305794bed03af5e3cedff7768bed3120261d6f3547a6d519943c2afcb80e423c9e6db088a06200dbfaa81edc5bc0de67957e791f67bf040ef944f7d62983d32517b2fb2d9572a71340c225617231bc0d98e66d19fe81a19b44280860b273f700bf3f3444a928e93fefc716e2af46995fbb658d0580a49e42f6835270c8c154abe28a17f76550b1b1fafe62945f80490b3f780fe9bb4d4b4107eac3d50b8c99d1a191f6754992096683fb0f599846bae759b06222079f5404be39e4416136c7c42255b0e7ca42d86fc2227892406d61f9816bc125d017989a671f13f2f4052e018b1fb02460802029a049a23d2ffeea6ac552109d35aa8731483fb2cae963987156056cafb32436a23a0dc918fb2440b14c9e6124441e7bb3b08706066d1ddab512267767b6e522f80732e67046ff5ad4d8193bf5cc5c05ccceb73a36b6c3ea39fa91bb308c8bb7bf88515d9c52409128e8b94e33e48a5396c35c20bd83b7c0e6d3d4a24bc14e84810066c686c6c04e687c41123fe87c89d5fa07b0095e7f82d3b07e72570163c47444bdde16ae9bfacd540df047e8ee34e98ff33178da5c7e6be9272e6dcfbb6db7e678a6d1d3832226c9bf85afa14feac15a270d5d3724a121b8fc9b40f0d37bb7f432de5421d286a65313a6efd251f7ed75b4ef6557975af5da5df2b87a0bbc1cb58183c4c1e24fdc4eb016777af1a6fa4a29d3eed7c4463482e591a6dc20540cabb6d7dd29cbb8ffdacafdaac2dd36db70fefe14fdeec85ef5fe01bb104d2d6439dbd7ceefc87007ce07b8409751dd7c21aa9a537f5fdefdef7d6ceba8d5ae876522f75dedd472e4dde1284e71380ee75ed313b2b9b9a94a56ebd03ae36b64a3b35abbdc7ba380016218201d156658ed9b5632f80f921879063e9037cd3509d01a2e91c17e03d892e2bc381ac723eba266497a1fbb0dc77ab3f4a9a981f95977b025b005a0e09b1add481888333927963fc5e5bf376655cb00e4ca8841fa450c8653f91cf2f3fb0247dbcace5dfde3af4a854f9fa2aaaa33706a78321332273ab4ee837ff4f8eba08676e7f889464a842b8e3e4a579d2".ToHexBytes();
            var data2 = "303163648162000100190130503701030000016f95973f840000000003b645f4927c25abe98be0b99f260c25cb3f712cdbad53f19a7b840297a2cafe6f7ef6733add48b133a12bf1b79059da8517d5a788f7df85c27c1c3dcfd3985de5dc01289483ff5a75c881bbcf4e348700159c194b3abef0e267d7ae9373947b5386708e21c17b9c63e8a7375441672754bc708299f9c9234834f9560c433a95e467a4e19ceba4344be62e4960a22ef4ec3665e79d8e561cd56f99b330334a84c860c77dcaa4bd989c29f19345af6996929871a4db81a54ebaacb846ec856f9afd661f5d2b632d7f49e6f2b6df7eec0d2df3bf7126db0e5622694861cf58c098520a17a010aaa9782b265bc9188c434405d00dfd70b98593d6937d3594549bdd0f4e2d1d1e4db56b8b5366fcb7d92f2a85c89af023eaffff6aa83417ffb9d463c5b6f5e2480dc022d50c7b7920295785c7b8af20a363e621a95018d9b595dc406a099f2221e1e10a2380776a701dc66c7da44d76d4b9d3aa1420ea30f366417a1f4ff34bb2b3aab2df46c45e0007cd2d571eb14de50a2ddf2a32bad57ab048278df10bfd25e1e557bd51c3e3e90e8c0951b11cb3611ed5f3939a4fed21c47b7a06cb447d7ac5cda7714eb35dc44a25b57faf3752e1b66d481b2d25b8fffb2ff0c27890162dc69180d9fb4ffffc3408ed41fb31dde78979f3ecffbea1c9e094480d838c91f36ce70498e6f1ff65803ffb67ecd1057f2a7648f1a4a4d190e447ed6d630b1d8c58808d0947fa463451f3f0002d03f269ef3d10679edbd8014bd0cf4a7c5d928be415498ec4356109d46dc4843f73e3c166087866e7f6b95de0fa7bca4b120fddc3d3a60fc09869353f5839c2cdc80270ae7f8014e940c403c75004cde11d1dbbbdc1ee5e1b1a7020fd053f7e2a2a30f926318d51f15893e5849a59df571f597557bc4cf2f192033a3b69643027ed6c538750f241be16e5652b66384d5d566604b2ac682df39284997cee7586a538ab9f37df53a2fd2ce547f94c11a6d301fd15b429cb57eac68b05c794f4c3db82f7f38a216adb592c0e09f16514018cb1d20ab36dd0e2bf96901a16fd6cdaf1dad409be144059554d3cd2562d332d9474498747a444f179298f74e72ff59e3c6a9727fd18a1d62e7f340ff93839a71509cad4d8fda099034e97eb3407ff91340f7590a0af188af847119a7fbe6e441da1dfb177be57a30018794f9ba6e131f5141636fb4df6117ce65148cf48e315739210373162239699ae8a3dc7ad624bdf818bc2230778990764c8c5f7cc13e3e74f1041e0be1b15bb0503a57091c3d89a6283820ecbaaa7ad76aa02df71a9979268a185a6a387216986651558e89d4f70fe18eff3b09a".ToHexBytes();
            var reader = new ReadOnlySequence<byte>(data1.Concat(data2).ToArray());
            SequenceReader<byte> seqReader = new SequenceReader<byte>(reader);
            long totalConsumed = 0;
            List<byte[]> packages = new List<byte[]>();
            while (!seqReader.End)
            {
                var header = seqReader.Sequence.Slice(seqReader.Consumed, 4);
                var headerValue = BinaryPrimitives.ReadUInt32BigEndian(header.FirstSpan);
                Assert.Equal(JT1078Package.FH, headerValue);
                if (JT1078Package.FH == headerValue)
                {
                    //根据数据类型处理对应的数据长度
                    seqReader.Advance(15);
                    if (seqReader.TryRead(out byte dataType))
                    {
                        JT1078Label3 label3 = new JT1078Label3(dataType);
                        int bodyLength = 0;
                        //透传的时候没有该字段
                        if (label3.DataType != JT1078DataType.透传数据)
                        {
                            //时间戳
                            bodyLength += 8;
                        }
                        //非视频帧时没有该字段
                        if (label3.DataType == JT1078DataType.视频I帧 ||
                            label3.DataType == JT1078DataType.视频P帧 ||
                            label3.DataType == JT1078DataType.视频B帧)
                        {
                            //上一个关键帧 + 上一帧 = 2 + 2
                            bodyLength += 4;
                        }
                        seqReader.Advance(bodyLength);
                        var bodyLengthFirstSpan = seqReader.Sequence.Slice(seqReader.Consumed, 2).FirstSpan;
                        //数据体长度
                        seqReader.Advance(2);
                        bodyLength = BinaryPrimitives.ReadUInt16BigEndian(bodyLengthFirstSpan);
                        //数据体
                        seqReader.Advance(bodyLength);
                        var package = seqReader.Sequence.Slice(totalConsumed, seqReader.Consumed - totalConsumed).ToArray();
                        packages.Add(package);
                        totalConsumed += (seqReader.Consumed - totalConsumed);
                        if (seqReader.End) break;
                    }
                }
            }
            Assert.Equal(2,packages.Count);
        }

        [Fact]
        public void Test3()
        {
            Assert.Throws<ArgumentException>(() => 
            {
                var data1 = "3031636481".ToHexBytes();
                var reader = new ReadOnlySequence<byte>(data1);
                SequenceReader<byte> seqReader = new SequenceReader<byte>(reader);
                while (!seqReader.End)
                {
                    if ((seqReader.Length - seqReader.Consumed)< 15)
                    {
                        throw new ArgumentException("not jt1078 package");
                    }
                    var header = seqReader.Sequence.Slice(seqReader.Consumed, 4);
                    var headerValue = BinaryPrimitives.ReadUInt32BigEndian(header.FirstSpan);
                    Assert.Equal(JT1078Package.FH, headerValue);
                    if (JT1078Package.FH == headerValue)
                    {
                        //根据数据类型处理对应的数据长度
                        seqReader.Advance(15);
                        if (seqReader.TryRead(out byte dataType))
                        {
                            JT1078Label3 label3 = new JT1078Label3(dataType);
                            if (seqReader.End) break;
                        }
                    }
                    else
                    {
                        throw new ArgumentException("not jt1078 package");
                    }
                }
            });
        }

        [Fact]
        public void Test4()
        {
            Assert.Throws<ArgumentException>(() =>
            {
                var data1 = "303163648162000000190130503701010000016f95973f840000000003b600000001674d001495a85825900000000168ee3c800000000106e5010d800000000165b80000090350bfdfe840b5b35c676079446e3ffe2f5240e25cc6b35cebac31720656a853ba1b571246fb858eaa5d8266acb95b92705fd187f1fd20ff0ca6b62c4cbcb3b662f5d61c016928ca82b411acdc4df6edb2034624b992eee9b6c241e1903bf9477c6e4293b65ba75e98d5a2566da6f71c85e1052a9d5ed35c393b1a73b181598749f3d26f6fbf48f0be61c673fcb9f2b0d305794bed03af5e3cedff7768bed3120261d6f3547a6d519943c2afcb80e423c9e6db088a06200dbfaa81edc5bc0de67957e791f67bf040ef944f7d62983d32517b2fb2d9572a71340c225617231bc0d98e66d19fe81a19b44280860b273f700bf3f3444a928e93fefc716e2af46995fbb658d0580a49e42f6835270c8c154abe28a17f76550b1b1fafe62945f80490b3f780fe9bb4d4b4107eac3d50b8c99d1a191f6754992096683fb0f599846bae759b06222079f5404be39e4416136c7c42255b0e7ca42d86fc2227892406d61f9816bc125d017989a671f13f2f4052e018b1fb02460802029a049a23d2ffeea6ac552109d35aa8731483fb2cae963987156056cafb32436a23a0dc918fb2440b14c9e6124441e7bb3b08706066d1ddab512267767b6e522f80732e67046ff5ad4d8193bf5cc5c05ccceb73a36b6c3ea39fa91bb308c8bb7bf88515d9c52409128e8b94e33e48a5396c35c20bd83b7c0e6d3d4a24bc14e84810066c686c6c04e687c41123fe87c89d5fa07b0095e7f82d3b07e72570163c47444bdde16ae9bfacd540df047e8ee34e98ff33178da5c7e6be9272e6dcfbb6db7e678a6d1d3832226c9bf85afa14feac15a270d5d3724a121b8fc9b40f0d37bb7f432de5421d286a65313a6efd251f7ed75b4ef6557975af5da5df2b87a0bbc1cb58183c4c1e24fdc4eb016777af1a6fa4a29d3eed7c4463482e591a6dc20540cabb6d7dd29cbb8ffdacafdaac2dd36db70fefe14fdeec85ef5fe01bb104d2d6439dbd7ceefc87007ce07b8409751dd7c21aa9a537f5fdefdef7d6ceba8d5ae876522f75dedd472e4dde1284e71380ee75ed313b2b9b9a94a56ebd03ae36b64a3b35abbdc7ba380016218201d156658ed9b5632f80f921879063e9037cd3509d01a2e91c17e03d892e2bc381ac723eba266497a1fbb0dc77ab3f4a9a981f95977b025b005a0e09b1add481888333927963fc5e5bf376655cb00e4ca8841fa450c8653f91cf2f3fb0247dbcace5dfde3af4a854f9fa2aaaa33706a78321332273ab4ee837ff4f8eba08676e7f889464a842b8e3e4a579d2".ToHexBytes();
                //var data2 = "303163648162000100190130503701030000016f95973f840000000003b645f4927c25abe98be0b99f260c25cb3f712cdbad53f19a7b840297a2cafe6f7ef6733add48b133a12bf1b79059da8517d5a788f7df85c27c1c3dcfd3985de5dc01289483ff5a75c881bbcf4e348700159c194b3abef0e267d7ae9373947b5386708e21c17b9c63e8a7375441672754bc708299f9c9234834f9560c433a95e467a4e19ceba4344be62e4960a22ef4ec3665e79d8e561cd56f99b330334a84c860c77dcaa4bd989c29f19345af6996929871a4db81a54ebaacb846ec856f9afd661f5d2b632d7f49e6f2b6df7eec0d2df3bf7126db0e5622694861cf58c098520a17a010aaa9782b265bc9188c434405d00dfd70b98593d6937d3594549bdd0f4e2d1d1e4db56b8b5366fcb7d92f2a85c89af023eaffff6aa83417ffb9d463c5b6f5e2480dc022d50c7b7920295785c7b8af20a363e621a95018d9b595dc406a099f2221e1e10a2380776a701dc66c7da44d76d4b9d3aa1420ea30f366417a1f4ff34bb2b3aab2df46c45e0007cd2d571eb14de50a2ddf2a32bad57ab048278df10bfd25e1e557bd51c3e3e90e8c0951b11cb3611ed5f3939a4fed21c47b7a06cb447d7ac5cda7714eb35dc44a25b57faf3752e1b66d481b2d25b8fffb2ff0c27890162dc69180d9fb4ffffc3408ed41fb31dde78979f3ecffbea1c9e094480d838c91f36ce70498e6f1ff65803ffb67ecd1057f2a7648f1a4a4d190e447ed6d630b1d8c58808d0947fa463451f3f0002d03f269ef3d10679edbd8014bd0cf4a7c5d928be415498ec4356109d46dc4843f73e3c166087866e7f6b95de0fa7bca4b120fddc3d3a60fc09869353f5839c2cdc80270ae7f8014e940c403c75004cde11d1dbbbdc1ee5e1b1a7020fd053f7e2a2a30f926318d51f15893e5849a59df571f597557bc4cf2f192033a3b69643027ed6c538750f241be16e5652b66384d5d566604b2ac682df39284997cee7586a538ab9f37df53a2fd2ce547f94c11a6d301fd15b429cb57eac68b05c794f4c3db82f7f38a216adb592c0e09f16514018cb1d20ab36dd0e2bf96901a16fd6cdaf1dad409be144059554d3cd2562d332d9474498747a444f179298f74e72ff59e3c6a9727fd18a1d62e7f340ff93839a71509cad4d8fda099034e97eb3407ff91340f7590a0af188af847119a7fbe6e441da1dfb177be57a30018794f9ba6e131f5141636fb4df6117ce65148cf48e315739210373162239699ae8a3dc7ad624bdf818bc2230778990764c8c5f7cc13e3e74f1041e0be1b15bb0503a57091c3d89a6283820ecbaaa7ad76aa02df71a9979268a185a6a387216986651558e89d4f70fe18eff3b09a".ToHexBytes();
                var data2 = "8162000100190130503701030000016f95973f840000000003b645f4927c25abe98be0b99f260c25cb3f712cdbad53f19a7b840297a2cafe6f7ef6733add48b133a12bf1b79059da8517d5a788f7df85c27c1c3dcfd3985de5dc01289483ff5a75c881bbcf4e348700159c194b3abef0e267d7ae9373947b5386708e21c17b9c63e8a7375441672754bc708299f9c9234834f9560c433a95e467a4e19ceba4344be62e4960a22ef4ec3665e79d8e561cd56f99b330334a84c860c77dcaa4bd989c29f19345af6996929871a4db81a54ebaacb846ec856f9afd661f5d2b632d7f49e6f2b6df7eec0d2df3bf7126db0e5622694861cf58c098520a17a010aaa9782b265bc9188c434405d00dfd70b98593d6937d3594549bdd0f4e2d1d1e4db56b8b5366fcb7d92f2a85c89af023eaffff6aa83417ffb9d463c5b6f5e2480dc022d50c7b7920295785c7b8af20a363e621a95018d9b595dc406a099f2221e1e10a2380776a701dc66c7da44d76d4b9d3aa1420ea30f366417a1f4ff34bb2b3aab2df46c45e0007cd2d571eb14de50a2ddf2a32bad57ab048278df10bfd25e1e557bd51c3e3e90e8c0951b11cb3611ed5f3939a4fed21c47b7a06cb447d7ac5cda7714eb35dc44a25b57faf3752e1b66d481b2d25b8fffb2ff0c27890162dc69180d9fb4ffffc3408ed41fb31dde78979f3ecffbea1c9e094480d838c91f36ce70498e6f1ff65803ffb67ecd1057f2a7648f1a4a4d190e447ed6d630b1d8c58808d0947fa463451f3f0002d03f269ef3d10679edbd8014bd0cf4a7c5d928be415498ec4356109d46dc4843f73e3c166087866e7f6b95de0fa7bca4b120fddc3d3a60fc09869353f5839c2cdc80270ae7f8014e940c403c75004cde11d1dbbbdc1ee5e1b1a7020fd053f7e2a2a30f926318d51f15893e5849a59df571f597557bc4cf2f192033a3b69643027ed6c538750f241be16e5652b66384d5d566604b2ac682df39284997cee7586a538ab9f37df53a2fd2ce547f94c11a6d301fd15b429cb57eac68b05c794f4c3db82f7f38a216adb592c0e09f16514018cb1d20ab36dd0e2bf96901a16fd6cdaf1dad409be144059554d3cd2562d332d9474498747a444f179298f74e72ff59e3c6a9727fd18a1d62e7f340ff93839a71509cad4d8fda099034e97eb3407ff91340f7590a0af188af847119a7fbe6e441da1dfb177be57a30018794f9ba6e131f5141636fb4df6117ce65148cf48e315739210373162239699ae8a3dc7ad624bdf818bc2230778990764c8c5f7cc13e3e74f1041e0be1b15bb0503a57091c3d89a6283820ecbaaa7ad76aa02df71a9979268a185a6a387216986651558e89d4f70fe18eff3b09a".ToHexBytes();
                var reader = new ReadOnlySequence<byte>(data1.Concat(data2).ToArray());
                SequenceReader<byte> seqReader = new SequenceReader<byte>(reader);
                long totalConsumed = 0;
                List<byte[]> packages = new List<byte[]>();
                while (!seqReader.End)
                {
                    if ((seqReader.Length - seqReader.Consumed) < 15)
                    {
                        throw new ArgumentException("not jt1078 package");
                    }
                    var header = seqReader.Sequence.Slice(seqReader.Consumed, 4);
                    var headerValue = BinaryPrimitives.ReadUInt32BigEndian(header.FirstSpan);
                    if (JT1078Package.FH == headerValue)
                    {
                        //根据数据类型处理对应的数据长度
                        seqReader.Advance(15);
                        if (seqReader.TryRead(out byte dataType))
                        {
                            JT1078Label3 label3 = new JT1078Label3(dataType);
                            int bodyLength = 0;
                            //透传的时候没有该字段
                            if (label3.DataType != JT1078DataType.透传数据)
                            {
                                //时间戳
                                bodyLength += 8;
                            }
                            //非视频帧时没有该字段
                            if (label3.DataType == JT1078DataType.视频I帧 ||
                                label3.DataType == JT1078DataType.视频P帧 ||
                                label3.DataType == JT1078DataType.视频B帧)
                            {
                                //上一个关键帧 + 上一帧 = 2 + 2
                                bodyLength += 4;
                            }
                            seqReader.Advance(bodyLength);
                            var bodyLengthFirstSpan = seqReader.Sequence.Slice(seqReader.Consumed, 2).FirstSpan;
                            //数据体长度
                            seqReader.Advance(2);
                            bodyLength = BinaryPrimitives.ReadUInt16BigEndian(bodyLengthFirstSpan);
                            //数据体
                            seqReader.Advance(bodyLength);
                            var package = seqReader.Sequence.Slice(totalConsumed, seqReader.Consumed - totalConsumed).ToArray();
                            packages.Add(package);
                            totalConsumed += (seqReader.Consumed - totalConsumed);
                            if (seqReader.End) break;
                        }
                    }
                    else
                    {
                        throw new ArgumentException("not jt1078 package");
                    }
                }
            });
        }

        public string ReadBCD(ReadOnlySpan<byte> readOnlySpan,int len)
        {
            int count = len / 2;
            StringBuilder bcdSb = new StringBuilder(count);
            for (int i = 0; i < count; i++)
            {
                bcdSb.Append(readOnlySpan[i].ToString("X2"));
            }
            return bcdSb.ToString().TrimStart('0');
        }

        [Fact]
        public void Test5()
        {
            var empty = "000000000".TrimStart('0');
            Assert.Equal("", empty);
        }
        [Fact]
        public void Test6()
        {
            string url = "https://www.baidu.com:8080/live.m3u8?aa=aa&bb=bb";
            var uri = new Uri(url);
            string filename = Path.GetFileName(uri.AbsolutePath);
            var name = Path.GetFileNameWithoutExtension(filename);
            var extension = Path.GetExtension(filename);
            var queryParams = uri.Query.Substring(1, uri.Query.Length - 1).Split('&');
        }

        class FixedHeaderInfo
        {
            public bool FoundHeader { get; set; }
            public int TotalSize { get; set; }
        }
    }
}
