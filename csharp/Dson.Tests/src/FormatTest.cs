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

using Wjybxx.Dson.Text;

namespace Wjybxx.Dson.Tests;

public class FormatTest
{
    private static readonly string dsonString = @"
            - {@{clsName:MyClassInfo, guid :10001, flags: 0}
            -   name : wjybxx,
            -   age: 28,
            -   pos :{@Vector3 x: 0, y: 0, z: 0},
            -   address: [
            -     beijing,
            -     chengdu
            -   ],
            -   intro: @ss
            |   我是wjybxx，是一个游戏开发者，Dson是我设计的文档型数据表达法，
            | 你可以通过github联系到我。
            -   thanks
            ~   , url: @ss https://www.github.com/hl845740757
            ~   , time: {@dt date: 2023-06-17, time: 18:37:00, millis: 100, offset: +08:00}
            - }";


    [Test]
    public void FormatTest0() {
        DsonObject<string> dsonObject = Dsons.FromDson(dsonString).AsObject();
        {
            DsonTextWriterSettings.Builder builder = new DsonTextWriterSettings.Builder() {
                ExtraIndent = 2,
                SoftLineLength = 50,
                TextStringLength = 50,
                StringAlignLeft = true
            };
            string dsonString2 = Dsons.ToDson(dsonObject, ObjectStyle.Indent, builder.Build());
            Console.WriteLine("Mode:" + DsonMode.Standard);
            Console.WriteLine(dsonString2);

            DsonValue dsonObject2 = Dsons.FromDson(dsonString2);
            Assert.That(dsonObject2, Is.EqualTo(dsonObject));
        }

        Console.WriteLine();
        {
            DsonTextWriterSettings.Builder builder = new DsonTextWriterSettings.Builder() {
                DsonMode = DsonMode.Relaxed,
                ExtraIndent = 2,
                SoftLineLength = 60
            };
            string dsonString3 = Dsons.ToDson(dsonObject, ObjectStyle.Indent, builder.Build());
            Console.WriteLine("Mode:" + DsonMode.Relaxed);
            Console.WriteLine(dsonString3);
            DsonValue dsonObject3 = Dsons.FromJson(dsonString3);
            Assert.That(dsonObject3, Is.EqualTo(dsonObject));
        }
    }
}