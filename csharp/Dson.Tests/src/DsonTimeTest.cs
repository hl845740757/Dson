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
using Wjybxx.Dson.Types;

namespace Wjybxx.Dson.Tests;

public class DsonTimeTest
{
    private const string DsonString = """
            - [
            -   @dt 2023-06-17T18:37:00,
            -   {@dt date: 2023-06-17, time: 18:37:00},
            -   {@dt date: 2023-06-17, time: 18:37:00, offset: +8},
            -   {@dt date: 2023-06-17, time: 18:37:00, offset: +08:00, millis: 100},
            -   {@dt date: 2023-06-17, time: 18:37:00, offset: +08:00, nanos: 100_000_000},
            - ]
            """;

    [Test]
    public void TestTime() {
        DsonArray<String> dsonArray = Dsons.FromDson(DsonString).AsArray();
        Assert.That(dsonArray[1], Is.EqualTo(dsonArray[0]));

        OffsetTimestamp second = dsonArray[2].AsTimestamp();
        OffsetTimestamp third = dsonArray[3].AsTimestamp();
        OffsetTimestamp fourth = dsonArray[4].AsTimestamp();
        // 偏移相同
        Assert.That(third.Offset, Is.EqualTo(second.Offset));
        Assert.That(fourth.Offset, Is.EqualTo(second.Offset));
        // 纳秒部分相同
        Assert.That(fourth.Nanos, Is.EqualTo(third.Nanos));
    }
}