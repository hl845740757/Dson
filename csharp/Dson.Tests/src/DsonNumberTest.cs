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

using NUnit.Framework;

namespace Wjybxx.Dson.Tests;

/// <summary>
/// 一测试就发现C#默认不支持Infinity....
/// 果然测试用例不够是不行的...
/// </summary>
public class DsonNumberTest
{
    private static readonly string NumberString = """
            - {
            - value1: 10001,
            - value2: 1.05,
            - value3: @i 0xFF,
            - value4: @i 0b10010001,
            - value5: @i 100_000_000,
            - value6: @d 1.05E-15,
            - value7: @d Infinity,
            - value8: @d NaN,
            - value9: @i -0xFF,
            - value10: @i -0b10010001,
            - value11: @d -1.05E-15,
            - }
            """;

    [Test]
    public void TestNumberParser() {
        Dsons.FromDson(NumberString);
    }
}