﻿#region LICENSE

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
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using NUnit.Framework;
using Wjybxx.Commons.Time;
using Wjybxx.Dson.Text;

namespace Wjybxx.Dson.Tests;

/// <summary>
/// 测试读写文件的性能（Release|AnyCPU）
/// 以540K的文件进行测试，结果如下：
/// <code>
/// StopWatch[System.Json=39ms][Read=30ms,Write=8ms]
/// StopWatch[Wjybxx.Dson=46ms][Read=36ms,Write=9ms]  // 禁用无引号字符串
/// StopWatch[Wjybxx.Dson=47ms][Read=35ms,Write=11ms] // 启用 MaxLengthOfUnquoteString 为 16
/// StopWatch[Bson=67ms][Read=48ms,Write=19ms]
/// </code>
/// ps：
/// 1. 本机设备信息：I7-9750H 2.6GHz  16G内存
/// 2. 后面测试了一下小文件(25k)，dson全面第一，耗时只有系统库的1/3。
/// 3. newtonsoft不能直接读写文件。。。因此不在此测试中。
/// </summary>
public class BigFileTest
{
    private FileStream NewInputStream() {
        return new FileStream("D:\\Test.json", FileMode.Open);
    }

    private FileStream NewOutputStream() {
        return new FileStream("D:\\Test2.json", FileMode.Create);
    }

    [Test]
    public void TestReadWriteFile() {
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

        // 系统库使用JsonObject或Object做泛型没有差异
        using FileStream inputStream = NewInputStream();
        object jsonObject = JsonSerializer.Deserialize<object>(inputStream);
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

        // Bson使用object做泛型会导致读性能骤降，降低1个Level 46ms => 160ms....
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