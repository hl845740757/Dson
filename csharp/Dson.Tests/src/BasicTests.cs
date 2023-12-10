using System.Diagnostics;
using Wjybxx.Dson.IO;
using Wjybxx.Dson.Text;

namespace Wjybxx.Dson.Tests;

/// <summary>
/// 测试三种Reader/Writer实现之间的等效性
/// </summary>
public class BasicTests
{
    // c#10还不支持 """，因此转为@格式
    private const string DsonString = @"           
            - @{clsName: FileHeader, intro: 预留设计，允许定义文件头}
            -
            - {@MyStruct
            -   name : wjybxx,
            -   age:28,
            -   介绍: 这是一段中文而且非常非常非常非常非常非常非常非常非常非常非常非常非常非常非常非常非常非常非常非常非常非常非常非常非常非常非常非常长 ,
            -   intro: ""hello world"",
            -   ref1 : {@ref localId: 10001, ns: 16148b3b4e7b8923d398},
            -   ref2 : @ref 17630eb4f916148b,
            -   bin : [@bin 0, 35df2e75e6a4be9e6f4571c64cb6d08b0d6bc46c1754f6e9eb4a6e57e2fd53],
            - }
            -
            - {@MyStruct
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
        DsonArray<string> topContainer1;
        using (IDsonReader<string> reader = new DsonTextReader(DsonTextReaderSettings.Default, DsonString)) {
            topContainer1 = Dsons.ReadTopContainer(reader);
        }
        string dsonString1 = Dsons.ToFlatDson(topContainer1);
        Console.WriteLine(dsonString1);

        // BinaryWriter
        {
            byte[] buffer = new byte[8192];
            IDsonOutput output = DsonOutputs.NewInstance(buffer);
            using (IDsonWriter<string> writer = new DsonBinaryWriter<string>(DsonTextWriterSettings.Default, output)) {
                Dsons.WriteTopContainer(writer, topContainer1);
            }
            IDsonInput input = DsonInputs.NewInstance(buffer, 0, output.Position);
            using (IDsonReader<string> reader = new DsonBinaryReader<string>(DsonTextReaderSettings.Default, input)) {
                DsonArray<string> topContainer2 = Dsons.ReadTopContainer(reader);

                string dsonString2 = Dsons.ToFlatDson(topContainer2);
                Debug.Assert(dsonString1 == dsonString2, "BinaryReader/BinaryWriter");
            }
        }

        // ObjectWriter
        {
            DsonArray<string> outList = new DsonArray<string>();
            using (IDsonWriter<string> writer = new DsonObjectWriter<string>(DsonTextWriterSettings.Default, outList)) {
                Dsons.WriteTopContainer(writer, topContainer1);
            }
            using (IDsonReader<string> reader = new DsonObjectReader<string>(DsonTextReaderSettings.Default, outList)) {
                DsonArray<string> topContainer3 = Dsons.ReadTopContainer(reader);

                string dsonString3 = Dsons.ToFlatDson(topContainer3);
                Debug.Assert(dsonString1 == dsonString3, "ObjectReader/ObjectWriter");
            }
        }
    }
}