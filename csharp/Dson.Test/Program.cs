using System.Diagnostics;
using Wjybxx.Dson.IO;
using Wjybxx.Dson.Text;

namespace Wjybxx.Dson.Test;

public class Program
{
    // c#10还不支持 """，因此转为@格式
    private static string dsonString = @"           
            - @{clsName: FileHeader, intro: 预留设计，允许定义文件头}
            -
            - {@MyStruct
            -   name : wjybxx,
            -   age:28,
            -   介绍: 这是一段中文而且非常非常非常非常非常非常非常非常非常非常非常非常非常非常非常非常非常非常非常非常非常非常非常非常非常非常非常非常长 ,
            -   intro: ""hello world"",
            -   ref1 : {@ref localId: 10001, ns: 16148b3b4e7b8923d398},
            -   ref2 : @ref 17630eb4f916148b,
            -   bin : [@bin 0, 35df2e75e6a4be9e6f4571c64cb6d08b],
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

    /// <summary>
    /// 程序生成的无法保证和手写的文本相同
    /// 但程序反复读写，以及不同方式之间的读写结果应当相同。
    /// </summary>
    /// <param name="args"></param>
    public static void Main(string[] args) {
        DsonArray<string> topObjects = new DsonArray<string>();
        using (IDsonReader<string> reader = new DsonTextReader(DsonTextReaderSettings.Default, dsonString)) {
            DsonValue dsonValue;
            while ((dsonValue = Dsons.ReadTopDsonValue(reader)) != null) {
                topObjects.Add(dsonValue);
            }
        }
        string dsonString1 = Dsons.ToDson(topObjects, ObjectStyle.Indent);
        Console.WriteLine(dsonString1);

        // BinaryWriter
        {
            byte[] buffer = new byte[8192];
            IDsonOutput output = DsonOutputs.NewInstance(buffer);
            using (IDsonWriter<string> writer = new DsonBinaryWriter<string>(DsonTextWriterSettings.Default, output)) {
                foreach (var dsonValue in topObjects) {
                    Dsons.WriteTopDsonValue(writer, dsonValue);
                }
            }

            IDsonInput input = DsonInputs.NewInstance(buffer, 0, output.Position);
            DsonArray<string> decodedDsonArray = new DsonArray<string>();
            using (IDsonReader<string> reader = new DsonBinaryReader<string>(DsonTextReaderSettings.Default, input)) {
                DsonValue dsonValue;
                while ((dsonValue = Dsons.ReadTopDsonValue(reader)) != null) {
                    decodedDsonArray.Add(dsonValue);
                }
            }
            string dsonString2 = Dsons.ToDson(decodedDsonArray, ObjectStyle.Indent);
            Debug.Assert(dsonString1 == dsonString2, "BinaryReader/BinaryWriter");
        }

        // ObjectWriter
        {
            DsonArray<string> outList = new DsonArray<string>();
            using (IDsonWriter<string> writer = new DsonObjectWriter<string>(DsonTextWriterSettings.Default, outList)) {
                foreach (var dsonValue in topObjects) {
                    Dsons.WriteTopDsonValue(writer, dsonValue);
                }
            }

            DsonArray<string> decodedDsonArray = new DsonArray<string>();
            using (IDsonReader<string> reader = new DsonObjectReader<string>(DsonTextReaderSettings.Default, outList)) {
                DsonValue dsonValue;
                while ((dsonValue = Dsons.ReadTopDsonValue(reader)) != null) {
                    decodedDsonArray.Add(dsonValue);
                }
            }
            string dsonString3 = Dsons.ToDson(decodedDsonArray, ObjectStyle.Indent);
            Debug.Assert(dsonString1 == dsonString3, "ObjectReader/ObjectWriter");
        }
    }
}