#region LICENSE

//  Copyright 2023-2024 wjybxx(845740757@qq.com)
// 
//  Licensed under the Apache License, Version 2.0 (the "License");
//  you may not use this file except in compliance with the License.
//  You may obtain a copy of the License at
// 
//      http://www.apache.org/licenses/LICENSE-2.0
// 
//  Unless required by applicable law or agreed to iBn writing, software
//  distributed under the License is distributed on an "AS IS" BASIS,
//  WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//  See the License for the specific language governing permissions and
//  limitations under the License.

#endregion

using System.Diagnostics;
using NUnit.Framework;
using Wjybxx.Dson.IO;
using Wjybxx.Dson.Text;

namespace Wjybxx.Dson.Tests;

/// <summary>
/// 测试三种Reader/Writer实现之间的等效性
/// </summary>
public class BasicTests
{
    // c#10还不支持 """，因此转为@格式
    internal const string DsonString = @"           
            - @{clsName: FileHeader, intro: 预留设计，允许定义文件头}
            -
            - {@{MyStruct}
            -   name : wjybxx,
            -   age:28,
            -   介绍: 这是一段中文而且非常非常非常非常非常非常非常非常非常非常非常非常非常非常非常非常非常非常非常非常非常非常非常非常非常非常非常非常长 ,
            -   intro: ""hello world"",
            -   ref1 : {@ref localId: 10001, ns: 16148b3b4e7b8923d398},
            -   ref2 : @ref 17630eb4f916148b,
            -   bin : [@bin 0, 35df2e75e6a4be9e6f4571c64cb6d08b0d6bc46c1754f6e9eb4a6e57e2fd53],
            - }
            -
            - {@{MyStruct}
            -   name : wjybxx,
            -   intro: ""hello world"",
            -   ref1 : {@ref localId: 10001, ns: 16148b3b4e7b8923d398},
            -   ref2 : @ref 17630eb4f916148b
            -  }
            -
            - [@{localId : 10001}
            -  [@bin 1, FFFA],
            -  [@ei 1, 10001],
            -  [@ei 2, null],
            -  [@eL 1, 20001],
            -  [@eL 2, null],
            -  [@ed 1, 0.5],
            -  [@ed 2, null],
            -  [@es 1, 10010],
            -  [@es 1, null],
            - ]
            -
            - [@{compClsName : ei, localId: 17630eb4f916148b}
            -  [ 1, 0xFFFA],
            -  [ 2, 10100],
            -  [ 3, 10010],
            -  [ 4, 10001],
            - ]";

    [SetUp]
    public void Setup() {
    }

    /// <summary>
    /// 程序生成的无法保证和手写的文本相同
    /// 但程序反复读写，以及不同方式之间的读写结果应当相同。
    /// </summary>
    [Test]
    public static void test_equivalenceOfAllReaders() {
        DsonArray<string> collection1;
        using (IDsonReader<string> reader = new DsonTextReader(DsonTextReaderSettings.Default, DsonString)) {
            collection1 = Dsons.ReadCollection(reader);
        }
        string dsonString1 = collection1.ToCollectionDson();
        Console.WriteLine(dsonString1);

        // BinaryWriter
        {
            byte[] buffer = new byte[8192];
            IDsonOutput output = DsonOutputs.NewInstance(buffer);
            using (IDsonWriter<string> writer = new DsonBinaryWriter<string>(DsonTextWriterSettings.Default, output)) {
                Dsons.WriteCollection(writer, collection1);
            }
            IDsonInput input = DsonInputs.NewInstance(buffer, 0, output.Position);
            using (IDsonReader<string> reader = new DsonBinaryReader<string>(DsonTextReaderSettings.Default, input)) {
                DsonArray<string> collection2 = Dsons.ReadCollection(reader);

                string dsonString2 = collection2.ToCollectionDson();
                Debug.Assert(dsonString1 == dsonString2, "BinaryReader/BinaryWriter");
            }
        }

        // ObjectWriter
        {
            DsonArray<string> outList = new DsonArray<string>();
            using (IDsonWriter<string> writer = new DsonCollectionWriter<string>(DsonTextWriterSettings.Default, outList)) {
                Dsons.WriteCollection(writer, collection1);
            }
            using (IDsonReader<string> reader = new DsonCollectionReader<string>(DsonTextReaderSettings.Default, outList)) {
                DsonArray<string> collection3 = Dsons.ReadCollection(reader);

                string dsonString3 = collection3.ToCollectionDson();
                Debug.Assert(dsonString1 == dsonString3, "ObjectReader/ObjectWriter");
            }
        }
    }

    [Test]
    public static void test_longStringCodec() {
        byte[] data = new byte[5200]; // 超过8192
        Random.Shared.NextBytes(data);
        string hexString = Convert.ToHexString(data);

        byte[] buffer = new byte[16 * 1024];
        IDsonOutput output = DsonOutputs.NewInstance(buffer);
        output.WriteString(hexString);


        IDsonInput input = DsonInputs.NewInstance(buffer, 0, output.Position);
        string string2 = input.ReadString();
        Debug.Assert(hexString == string2);
    }
}