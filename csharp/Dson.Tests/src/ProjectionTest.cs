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

using NUnit.Framework;
using Wjybxx.Dson.Text;

namespace Wjybxx.Dson.Tests;

public class ProjectionTest
{
    private const string DsonString = """
            - {@{clsName:MyClassInfo, guid :10001, flags: 0}
            -   name : wjybxx,
            -   age: 28,
            -   pos :{@{Vector3} x: 1, y: 2, z: 3},
            -   address: [
            -     beijing,
            -     chengdu,
            -     shanghai
            -   ],
            -   posArr: [@{compClsName: Vector3}
            -    {x: 1, y: 1, z: 1},
            -    {x: 2, y: 2, z: 2},
            -    {x: 3, y: 3, z: 3},
            -   ],
            ~   url: @sL https://www.github.com/hl845740757
            - }
""";

    private const string ProjectInfo = """
          {
            name: 1,
            age: 1,
            pos: {
              "@": 1,
              $all: 1,
              z: 0
            },
            address: {
              $slice : [1, 2] //跳过第一个元素，然后返回两个元素
            },
            posArr: {
              "@" : 1, //返回数组的header
              $slice : 0,
              $elem: {  //投影数组元素的x和y
                x: 1,
                z: 1
              }
            }
          }
""";


    [Test]
    public void Test() {
        DsonObject<String> expected = new DsonObject<string>();
        {
            DsonObject<String> dsonObject = Dsons.FromDson(DsonString).AsObject();
            transfer(expected, dsonObject, "name");
            transfer(expected, dsonObject, "age");
            {
                DsonObject<String> rawPos = dsonObject["pos"].AsObject();
                DsonObject<String> newPos = new DsonObject<string>();
                transfer(newPos, rawPos, "x");
                transfer(newPos, rawPos, "y");
                expected["pos"] = newPos;
            }
            {
                DsonArray<String> rawAddress = dsonObject["address"].AsArray();
                DsonArray<String> newAddress = rawAddress.Slice(1);
                expected["address"] = newAddress;
            }

            DsonArray<String> rawPosArr = dsonObject["posArr"].AsArray();
            DsonArray<String> newPosArr = new DsonArray<string>(3);
            foreach (DsonValue ele in rawPosArr) {
                DsonObject<String> rawPos = ele.AsObject();
                DsonObject<String> newPos = new DsonObject<string>();
                transfer(newPos, rawPos, "x");
                transfer(newPos, rawPos, "z");
                newPosArr.Add(newPos);
            }
            expected["posArr"] = newPosArr;
        }

        DsonObject<String> value = Dsons.Project(DsonString, ProjectInfo)!.AsObject();
        Console.WriteLine(Dsons.ToDson(value, ObjectStyle.Indent));
        Assert.That(value, Is.EqualTo(expected));
    }

    private static void transfer(DsonObject<String> expected, DsonObject<String> dsonObject, String key) {
        expected[key] = dsonObject[key];
    }
}