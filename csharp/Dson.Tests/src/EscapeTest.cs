#region LICENSE

// Copyright 2023-2024 wjybxx(845740757@qq.com)
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

using System.Text;
using NUnit.Framework;
using Wjybxx.Dson.Text;

namespace Wjybxx.Dson.Tests;

public class EscapeTest
{
    private const string RegExp = "^[\\u4e00-\\u9fa5_a-zA-Z0-9]+$";

    /// <summary>
    /// C#的 """ 规则与Java不一样....
    /// '\'不需要转义...又让我测试了半天...
    /// </summary>
    private const string DsonString = """
            - {
            #   @ss 纯文本模式下输入正则表达式
            -   reg1: [@es 10, @ss ^[\u4e00-\u9fa5_a-zA-Z0-9]+$
            ~   ],
                        
            #   在纯文本模式插入转义版本的正则表达式
            -   reg2: [@es 10, @ss
            ^ ^[\\u4e00-\\u9fa5_a-zA-Z0-9]+$
            ~   ],
                    
            #   在双引号模式下插入纯文本的正则表达式，结束行需要使用 '|' 否则会插入换行符导致不等
            -   reg3: "
            ^ ^[\u4e00-\u9fa5_a-zA-Z0-9]+$
            | "
            - }
            """;

    [Test]
    public void TestEscapeMode() {
        // StreamReader streamReader = new StreamReader(new FileStream("D:\\Test.json", FileMode.Open), Encoding.UTF8);
        // using DsonTextReader reader = new DsonTextReader(DsonTextReaderSettings.Default, streamReader);
        
        DsonValue value = Dsons.FromDson(DsonString)!;
        DsonExtString reg1 = value.AsObject()["reg1"].AsDsonExtString();
        Assert.That(reg1.Value, Is.EqualTo(RegExp));

        DsonExtString reg2 = value.AsObject()["reg2"].AsDsonExtString();
        Assert.That(reg2.Value, Is.EqualTo(RegExp));

        DsonString reg3 = value.AsObject()["reg3"].AsDsonString();
        Assert.That(reg3.Value, Is.EqualTo(RegExp));

        Console.WriteLine(value.ToDson());
    }
}