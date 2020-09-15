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

        [Fact(DisplayName ="处理粘包问题")]
        public void Test1_3()
        {
            var reader = new ReadOnlySequence<byte>("30316364816200000019013050370101000001748D1EE0940000000003B600000001674D001495A85825900000000168EE3C800000000106E501B1800000000165B800000DEC70BFEA0866F2FF03B91EB94A749F7EDCFAE5017C7F2BDBD355BE0C0400001D31335CCBA5248D1AEA787885C3837C89BD62F06A4B29F4AB58E277BFF3CB03994DEBF8F362A3D27AFB63414B02F9C2914F304309BFB5DE324BC343C90D68B379120BE1FFEE7FC5269C7B90FBE40613A151EA390402436C06F60DFEEE6140B20FC30EE196E428B8A2F0399610229D26DB484BC043FC008FC5F08FEFE72F5046BF802738C08D91B7BAA9EE130B7363423A4F67D0A4B54068ABE1A763C39D0FC629219122EFBDC7E6D55132616BA98E16676D30AEE4083FA4B817CAF21C66724A7D0CE49EA0432FB2C899AB864E0B2E7F5D5D03D044C6BA54875D6D1CD0A04FF8BD0A2AE9EFC437D6F8DA15E012294BC3C71E31B0E59570A9424CB02BA7B1FFDFB0EF18DA213A6C8FA9970ED5E59CA411510B4028E19BB0A1734CB71FFF62ACD94A61D919A0AB33955FF32339CC056477D4BD7B6890EC93AE745B7F7FFFE6EB13F7EFB84C453ADB155A07F7E7C232F348EBF9E9C8EDCF92F8F538AB755CD1F8E244709BA04ECB76A6FFC08E7D242CB1F06CCDA9BF157CF237FFD6156DAEC6D258475C665EDD6FC29889315EB572BB5FAB7F662BA2E1D55BAEC5C2FD21F4FD4CF32414A6E2C343E3AD8C7446DE53C514F3995F97F60184565D318FC8BE1ADDDB28FD70E0D70E7365DFB6650F3279CC595B654C3843D762F62B765F5AC6A0495E440BBBCFC2061D74039368F51DC0C012B50A8507D56D4DDDD558980358B4AD7A81427171997B75BF0D09E706507B99A967D4297724533F583962DE7F138B3C75626FEB225F2B9E87A864F569CC1B3BB948887462F3F02896E8EF4DB06D36A8238125C9D28F41B4DD373BE2C1F38AED886C1F9DEFE4DCEAEA32769D489C33949E4F2886F8114B87C1C0D20DA931AD4A78A080DCE229537843A1BF780A7838114ED266B7B1C87D573AE58E29516905596773C6A1FE9AC4E0D62C93162E9025C6221A3E67C050B8D192796EC864D0F868A6CAD393F5A3343F442497D494375CBF6626A0D36BDDF1417A394B249A2E6EB3D3B833793C7E5E1D1E7CE5791AD81EB492996A57A9DDACEC7E1CDFC536133B9C5D5F7EF56E236ECC0537ACBB265DD55D607FDBEE915E2ED2C44A778C166C3C90E779D72C714BFED329F51CA4334EFBAB1C88E1E71D24D0E14CEF1A87D1D24E0EF15E5471D54F8EA50F92456C3D90326A5C43FF8AD48B08F829EBE70A8BD0D3086C7DC709D1488CDE3E1FBD68929C929F4558681FC068750036BC9522B1A870CF81DA30316364816200010019013050370103000001748D1EE0940000000003B697E4D8144D57E3D7E5D297AAC64DFC135FD34E84EA8EEB6723D3A68156F270798F619F4BF102A1A04B066C1D6BABAF0E5B036293B2350370A081D998EF937EBBDCEB48769306CDA526781E849D13DAE239FFBA5301F8D4764EE7AD9437FF38586B56C667AF6C9F8B461DC87418AC2E2B2B9B2B1B0E5F3F38C46E314CFB1F498E9ECAAEE523261F41294700F1FFFF272A22C93FFD1B4063CFA4279A9BC156BD54CA317CB8AFC4967E3DF7ADA6336B2B55187DA0EFC7980B44F2F53BE5617BEDF580270DF530B58AC028F9E9E7E0748C92A047916F25EE831CA91DC01F14C36FE7CE066EC5DF90E3EFC40F7D797F019A216C9D2AC43947B5BF2DDE43C47BE2F8C7E6C7F2E26DBCCCD850F38413C7B0D6C2843E25A7A311EB65E06F967FF6AAE2743BF427C9B2F2111483EC572548F965E40A056FD96B8B5036112F94A6F998BAEF1B99342918E2AA9D986DB524D3A2383363B630887DE73CE6119C585EFA1BC70761BEA61A5756520555DE14504463912BDD8388429257C497A5B5".ToHexBytes());
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
                        Assert.Equal(950, bodyLength);
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
                if ((seqReader.Remaining - fixedHeaderInfo.TotalSize) < 0) break;
                seqReader.Advance(fixedHeaderInfo.TotalSize);
                var package = seqReader.Sequence.Slice(totalConsumed, seqReader.Consumed - totalConsumed).ToArray();
                packages.Add(package);
                totalConsumed += (seqReader.Consumed - totalConsumed);
                if (seqReader.End) break;
            }
            Assert.Single(packages);
        }        
        [Fact(DisplayName ="处理粘包问题1")]
        public void Test1_4()
        {
            var reader = new ReadOnlySequence<byte>("3031636481620000001901305037020100000174919058A50000000003B600000001674D001495A85825900000000168EE3C800000000106E50147800000000165B800000D11E0BFEA0866F2FF03B91EB94A749F7EDCFAE5017C7F2BDBD355BE0C0400001D31335CCBA5248D1AEA787885C3837C89BD62F06A4B29F4AB58E277BFF3CB03994DEBF8F362A3D27AFB63414B02F9C2914F304309BFB5DE324BC343C90D68B379120BE1FFEE7FC5269C7B90FBE40613A151EA390402436C06F60DFEEE6140B20FC30EE196E428B8A2F0399610229D26DB484BC043FC008FC5F08FEFE72F5046BF802738C08D91B7BAA9EE130B7363423A4F67D0A4B54068ABE1A763C39D0FC629219122EFBDC7E6D55132616BA98E16676D30AEE4083FA4B817CAF21C66724A7D0CE49EA0432FB2C899AB864E0B2E7F5D5D03D044C6BA54875D6D1CD0A04FF8BD0A2AE9EFC437D6F8DA15E012294BC3C71E31B0E59570A9424CB02BA7B1FFDFB0EF18DA213A6C8FA9970ED5E59CA411510B4028E19BB0A1734CB71FFF62ACD94A61D919A0AB33955FF32339CC056477D4BD7B6890EC93AE745B7F7FFFE6EB13F7EFB84C453ADB155A07F7E7C232F348EBF9E9C8EDCF92F8F538AB755CD1F8E244709BA04ECB76A6FFC08E7D242CB1F06CCDA9BF157CF237FFD6156DAEC6D258475C665EDD6FC29889315EB572BB5FAB7F662BA2E1D55BAEC5C2FD21F4FD4CF32414A6E2C343E3AD8C7446DE53C514F3995F97F60184565D318FC8BE1ADDDB28FD70E0D70E7365DFB6650F3279CC595B654C3843D762F62B765F5AC6A0495E440BBBCFC2061D74039368F51DC0C012B50A8507D56D4DDDD558980358B4AD7A81427171997B75BF0D09E706507B99A967D4297724533F583962DE7F138B3C75626FEB225F2B9E87A864F569CC1B3BB948887462F3F02896E8EF4DB06D36A8238125C9D28F41B4DD373BE2C1F38AED886C1F9DEFE4DCEAEA32769D489C33949E4F2886F8114B87C1C0D20DA931AD4A78A080DCE229537843A1BF780A7838114ED266B7B1C87D573AE58E29516905596773C6A1FE9AC4E0D62C93162E9025C6221A3E67C050B8D192796EC864D0F868A6CAD393F5A3343F442497D494375CBF6626A0D36BDDF1417A394B249A2E6EB3D3B833793C7E5E1D1E7CE5791AD81EB492996A57A9DDACEC7E1CDFC536133B9C5D5F7EF56E236ECC0537ACBB265DD55D607FDBEE915E2ED2C44A778C166C3C90E779D72C714BFED329F51CA4334EFBAB1C88E1E71D24D0E14CEF1A87D1D24E0EF15E5471D54F8EA50F92456C3D90326A5C43FF8AD48B08F829EBE70A8BD0D3086C7DC709D1488CDE3E1FBD68929C929F4558681FC068750036BC9522B1A870CF81DA3031636481620001001901305037020300000174919058A50000000003B697E4D8144D57E3D7E5D297AAC64DFC135FD34E84EA8EEB6723D3A68156F270798F619F4BF102A1A04B066C1D6BABAF0E5B036293B2350370A081D998EF937EBBDCEB48769306CDA526781E849D13DAE239FFBA5301F8D4764EE7AD9437FF38586B56C667AF6C9F8B461DC87418AC2E2B2B9B2B1B0E5F3F38C46E314CFB1F498E9ECAAEE523261F41293A8405457961C29F4AAFED82D018F3E909E6A6F055AF55328C5F2E2BF2AE2F6B6F19591C59B10E335710E68D25230A539E2E6017C94B66E7527F077C506542C380FD6202D84BE1BFCBD574B4A63A27C69335555D930F6B8B680D41EA54E6C34E68E577103DF5E5FC0667E8AAA88278D7DF39E2F0F7E203C6A745A051695CC76F9A713EE3C5EECE82E5FB94055A9D7CB104FB36FF41B3F4EAF8CF65E3CA38877CF48465F58A676A097D85EA86B5620139EA3BE8CBAF9FBE34FF7B0114A064C3570C179D422038BF15E722456B94EE449BD4A9BE45ED3AC90720C9D2A3BF41A64CB747872054AEA1002C4A4501C4D19B102568C1C9F306A0DF9378E398271B0BDEAFBC639AA5D0930E41523F6E89AA1FD3AA41D68EC60382FDCED5E97EFC06750BBAD702D7EADA22388695C620DCB3478B0D3BD1194160519EBF98F358A5F184AD91E9F2CD9C0E425340720116E8F81EC159AB1733A5AD5050BED4E6CE45EA21290D4548EEC0F1359610AED616AC7072F69DFA947C81BA046E561A1F8A6F3451EE3C87D94A01A5AA2F4F0A305AB3EDEBA321749482C796F51F2241C6F5866D5E20F94B4D76C9595867526F454DCC3C08ED550651C60DABFC6F97BF890B416789A955202781088A8DB1D79E2F40D2B1CD5A2F06D47D4CF30B3C4276404EDD7EF50321DAE180E120D47015833AD8B4A3CB364F035C7DF2C44CA62BED73B6AA3162007BE3E9DEC78462E100EEC1EDFCA4BB58D6CDBCE49A53401CDDC4D8224A9E455CAA11E6BE408D57799D371B9C42F8852430D3C3E162834DE8A1BC434629CC6A4B9717E9036F2640CF9E3093DFBB756D187D626917AB0DD1B35CF728D2F8A9AEEC1D667D4CD7BC253C23867A1CD9DF2178E2CA8AB209FA1350AC884B6F45526AA3123738A8D622035FD886A1CDE7282B6C57E03CCF62BD677643F510BD4B1D68EDE5872EE9C2561D9CCAF6D6C69666FF63816897ABEB4DBF15CDD22F494BA30415201B8E47888C4BC0CC6B87659A0C9B81E2C32242942E2B67882AFE3F3C27B24BA0989C92FB9F50C6C7D3106B67278CCEF089380D8F3669F110314AB6D553096B87319668D0451E7BA792FFDD4595A1E687AD692B392654F189361B82F7B578FD07E53D0EDD3A6EEBB77C2A8B7F3031636481620002001901305037020300000174919058A50000000003B6BEFA8F8B050FDF960219FC6907F1954A817D80C8818354E4F2BB900818F3919B30CD4FAD26C3F364EC998FEC08E57301C5D51A4C7903DD56BB252D17D94DDC1F4FF5CA656077B500FF9B86740A95E33282D7BD34034B0E868115788558935AB02AF6535BF370374FC89A79B71E914BCF58500A7782ACC72FD26679CB728C007373F8BBA415F85AE8970199C16812344764F18D37A8894F5D4F94C50DBE396BDA52F47B5DD725A3979AF15BDCA613D3D3309750056B9477698B423D9E7899D57BF5C30537687D05D1B62D69B7050DDEC0ABC36CACBBBDB1FD16DE675CABF676663BE45B88BE39DAB884F80A7D11DCC5CADF1A4E0ACC8DF8F1C98D310FEAEAABAAB567D375083D229FF1E703B7A63026270186E7C96D5D848949DD6A944B4691758DEDBE7544317BCF10BA44BA2F0F1FF86EC04F7068CCAB623F820AACB4114125CE55441C13AFB1CC25E4B374439BD803256860E04DAC8EE21391F6050953314F598A518EA69A7DD79646B101388AA418BA310FB1FEEB7C6EBDED0F61A7A318FDC5F6614D306D62120B285222E7808F57430B9EDD6471F3A079FF7570A77FDB410600097AA80BE2441EE7C9B21350163F1B960F83A9C2D57FC4A03356DE9D76FAE7A62B01ABF70DA830B442F0ECD28A4FACFE4825C0C33380EF0DB7D193E4C8DB7AEBA3903B2C09DC9773BC1D952B2BE778FB0A5D56746FB07FFEB9510B07CE5280F8EAABB8E83C5D8A822D5187322369BB9C43EC329ED00265201AF84F658A69E6BA641882D838ADB38850AF3E9127FC864C0C3F2DF83945CA1EDE3F5754FCED659AF9CDB5969EFFFD61C74D128268195F6637749518DAEAA6BFE649230E3F6262DB1848EF2957270FF4E254FE2D38B4E7B5CC9DB8870DCEF4248964706B7EFAFA268A65E793EC5A6679F82986450798615A45EF5142A65F5FF0123EABDC08690CB5DE77CF06344BF95ED1096BA2D18222A57E5056E2A4B687EE84A78C36E57F160060AC82E6B53641EF3BEE9360C96AE0909EE40BA4C1151BD5D932E57208D5F97E6F3DFDC1B13059753A774D131768B78A42F8AA323C7FF12ABA4D106F131A16F0D322BFE6BF11C4830E0B2DEF231B76967C15397940C703C862BFEF11C9B2C6189CC13B325B42A98FA03A50817CA6FC7F2CCEF44FE598E4F31F1B17F4C24BC2910A4D9D2C5F2AE3D3B481293E2F600173E997326CB869E023305D7C8A1BB883DE8939C63AF4764316F7DF1AF0BA89711C397F9E32E310ABB06857D341A56E73CFCD0C07CB435CC86367C6732FAE921D22F833A474F8C43250CC49163B70BF3086C3D88EC7B5F71786B801FAF475B32E82F01F87363031636481620003001901305037020300000174919058A50000000003B62FEFB493EEAD2F877362AA8AE88AA00A645331CF7EDC0376CA2F87E14AC39C64451268DA20FAAD0D7D34F9012F63BCF5D1ED371717D677CD844495649998C22AD6DD71D0D36F370EBC1FBEDBDA260E995C2DD5C2CCD9DF9E03812A4551DE57A3707B95478C50F5DC00000300EBC23B0DF38999E0675FE239025F787BF85977BAD1DE911848C91CA4E4D57B63EC4E2C948CAB01AF5458C7A6FF48DC529721E082B4AA2A3D61812742509A523659C0F874B0D8BB35E72A12E9CC813B92050ABB00A9C941DB46EAE089B73CCDCCD65061E6106E605FC8BC8FFB76D8C404515DCAB3E190D4E344F15D889F1EDCE41F080C9A084760A99C700C35C4BB7B1DEBDCAF154899CE386157C274DDC0742029AD9F5BBFE3167D8ED2EB12DA008862CD262A659DDAA4FDDF3BD8F7821D2EFF0C36AF1C2CCC7020FC4C066AE93A0FC3DB4353A2DDB92BA57AF3FC78D8721EA309206B2216549188A14635B4DD420D00781EAE8B74B2E48077544EA05BDA0CB672950615D491F142ECCB19E502615D01B361BCD2778203B3B9F3B09A68032DA6FB2B4537E5254C9AF52359B41405581ABD5E66637B851A4ACE70D5D2B869DA8185C7149FFE08AD498E881C80637FCCB885078C5782217232346A19EE818862A631B38B9D59AF03DDB619025AD35BA313C56C8272857C8551811DC848C512D6268F12C14509A1EEF8A0BD07DF82C779FDBFEB7392FB5B4FC0BEA232C7742A26CA6E594B192F9C07B9973211C7FDEF01558EBFA8DEAE06C7E112E001C7E829337FFF6C242B643F687A79D9FC2989D003285EBDFAC50ADAA34A9DCF360B4FC1703C7DB43514E19158828C59176CDFCF3A4F2DAF4BB67617DF1730DE3C56EB657AA564D7B4C7BE79F24406C6CB07FDFCCF29A0929AF4065ECCDA5DD813120AE6D641846025A51E9CC9F1CCE9D32E020068F291CD492C539ACFC0630D30EC32A6ACFC187CD3CBB062F43D6BC332DBB1F9ABAD7E2174DD38316FC18193966BAEA27B374C88768C86DEE7237EDDB426B078504F722F57380D87AF54DD33ADB35200FC61EDC9ADFED175F403C92E1B11BBE3E9DAC3D36328860C567F2495648E1B9D93086E3AA602EBDDF12EAA7621A7F3341C1D730B52989B186EAAABA732BF703C5D281249E3442D48B20E139BCBECDD7E33F5863638CC6CAFF2F0D59979449DBD0B224DB8AFD4949917DE2D8A48106A5F61F8A83F041FF88E732D802354FB2088B3A3FB110A8894D9E5838944980E203772A25CD133E20C522E4E69EBB9BC04AB5CD8DDCCC27903810A0A5AF4FA470276519B18397B99D49A8A29A4B18A41C8E56FB8D47A498DB2994A388B8C3031636481E20004001901305037020200000174919058A500000000039146C98C2F4E71B29E8DE430849F3F498389A45288CBB50DD2DE082A513A890B05DF2370358B918159C3AFAF07EC60533CC2CF10AEF2DDCF2540A277A2862A37037CE1EB45051159ABFCE64ABBC9F31134449DB7CDEAD1E417FE1FDF2CC475DC8398A5F1DC7F66232A7AA121A9F2C3951E8735AB3E85E3FF7AC59288A07FE9679F761E023F7747A589D6C1B4007E036C071949B0539F1D75448E2C4F8D1A9C78FB814E161C22D4991AA8AA521E7740599EB3DBD2A66C0E209739A459B4F1CE35A6DE4043F427E2B7F9D7BA49256005204C838650C3B57C59DA58894ED735C15107F79C42594F6240900275500735B081911F9E3E4DC343F56FEB318EB1AE877EF7AE8DA3B6BD8450B378718123EC021E097F6B8F6FE3DBD27954F1BB65BB2333CBCAC72201618B868E443436CF62F3169B50F9FB2F64B90206DE8C21EAA419896D4D13550D92CCE732CE32867306F97E202FC0ED84F7037C47D84D314D43DE5DA00AADB292E7BCE2C4525A73B5A781C511B1E576C96CEB81E21B8BAC044A7F0A586599E6B7880C5E6A1219DDFAAEA48F8B1CF541004BF8A57193B853DA36E8948655080DF218DABEB7EA271A50DAFC07A6CA772B8B9534F89541E5E0D95B1BF1606B38455C1C686496A190656501C212D16F018B2E0C6C3B86815308AD24701DABD6FF1F1CDE53E9CBF3C014E7EA60875C3FCD8609F5DF592187808FEF158627FF2BDABDB3E3BC619894E965C90BBA4DAF9C1D2562BBB5A05EF24CD3A9E7F160A86E9CFB7F3A7A125204044E541DECCC5D0C9C6526D2EC72E6A09295596D918325CD3D85A373D092A4753149E1F0988A11956D1BC45FA85CAFAB053816A8BA7DCE3A2FF460E2B97CF916A4456A54F4165F187C920145393CFB034CD45A2A09818483E403E2F614AECFA888AF8255F2F56DBB570A12F10BB46F8A623A085FCF63C44D02197D9644445B380142E813627433F916A1891B35A575B68D9C7BAC9498E5C3BB3E9701BB47CEB4E5EBAC82E9F1D2FF5A2C441E538915FAB44B9E9C0BA93F2317D07C3C27A5B16A66458BFCC13D1696C4DD1C38D8409AC60B3177458757F35CFFE6B94EDEC2286114AE312A7D86B92A6CD7994602A0E751113C9597369B1C8DB07E9EFCB9B91F055D2E9EB08F6E61E2B55503C07DF2841E53FEEDABA94CFABBA39A3B18C6F35AF698BC1E3F2F0E372516D31CF8DCC7D7501BD2990558C759D74EEBEECA9D12342F78E814ADB7CB3AAE9B7CE0FCAFBB157261C7FD72FEFFFDEEEC0310FFA8A9ECD22CA40D69F706A20C".ToHexBytes());
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
                if ((seqReader.Remaining - fixedHeaderInfo.TotalSize) < 0) break;
                seqReader.Advance(fixedHeaderInfo.TotalSize);
                var package = seqReader.Sequence.Slice(totalConsumed, seqReader.Consumed - totalConsumed).ToArray();
                packages.Add(package);
                totalConsumed += (seqReader.Consumed - totalConsumed);
                fixedHeaderInfo.Reset();
                if (seqReader.End) break;
            }

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
            public void Reset()
            {
                FoundHeader = false;
                TotalSize = 0;
            }
        }
    }
}
