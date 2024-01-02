#region LICENSE

// Copyright 2023 wjybxx(845740757@qq.com)
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//     http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

#endregion

using System.Text.Json;
using System.Text.Json.Nodes;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using NUnit.Framework;
using Wjybxx.Commons.Time;
using Wjybxx.Dson.Text;

namespace Wjybxx.Dson.Tests;

public class BigFileTest
{
    private FileStream NewInputStream() {
        return new FileStream("D:\\Test.json", FileMode.Open);
    }

    private FileStream NewOutputStream() {
        return new FileStream("D:\\Test2.json", FileMode.Create);
    }

    /// <summary>
    /// 发现Dson的速度比系统库的Json慢不少...
    /// Dson的Reader稍优于Bson的Reader -- 毕竟我们算法实现基本一致。
    /// 
    /// Dson的Writer是3个里最慢的，Reader比Bson块一丢丢
    /// 不过，我们的测试输入全部是字符串值...猜测是字符串处理有问题
    /// </summary>
    [Test]
    public void TestReadWrite() {
        if (!File.Exists("D:\\Test.json")) {
            return;
        }
        TestSystemJson();
        Thread.Sleep(1000);

        TestDson();
        Thread.Sleep(1000);

        TestBson();
    }

    private void TestSystemJson() {
        StopWatch stopWatch = StopWatch.CreateStarted("System.Json");

        using FileStream inputStream = NewInputStream();
        JsonObject jsonObject = JsonSerializer.Deserialize<JsonObject>(inputStream);
        stopWatch.LogStep("Read");

        using FileStream outFileStream = NewOutputStream();
        JsonSerializer.Serialize(outFileStream, jsonObject,
            new JsonSerializerOptions
            {
                WriteIndented = true,
            });
        stopWatch.LogStep("Write");
        Console.WriteLine(stopWatch.GetLog());
    }

    private void TestDson() {
        StopWatch stopWatch = StopWatch.CreateStarted("Wjybxx.Dson");

        using DsonTextReader reader = new DsonTextReader(DsonTextReaderSettings.Default, new StreamReader(NewInputStream()));
        DsonValue dsonValue = Dsons.ReadTopDsonValue(reader)!;
        stopWatch.LogStep("Read");

        DsonTextWriterSettings settings = new DsonTextWriterSettings.Builder
        {
            DsonMode = DsonMode.Relaxed,
            EnableText = false,
            MaxLengthOfUnquoteString = 0,
        }.Build();

        using DsonTextWriter writer = new DsonTextWriter(settings, new StreamWriter(NewOutputStream()));
        Dsons.WriteTopDsonValue(writer, dsonValue);
        stopWatch.LogStep("Write");
        Console.WriteLine(stopWatch.GetLog());
    }

    private void TestBson() {
        StopWatch stopWatch = StopWatch.CreateStarted("Bson");

        using FileStream inputStream = NewInputStream();
        BsonDocument bsonDocument = BsonSerializer.Deserialize<BsonDocument>(new JsonReader(new StreamReader(inputStream)));
        stopWatch.LogStep("Read");

        using JsonWriter jsonWriter = new JsonWriter(new StreamWriter(NewOutputStream()), new JsonWriterSettings()
        {
            Indent = true
        });
        BsonSerializer.Serialize(jsonWriter, bsonDocument);
        stopWatch.LogStep("Write");

        Console.WriteLine(stopWatch.GetLog());
    }
}