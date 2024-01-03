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

using System;
using System.Collections.Generic;
using System.Text;

#pragma warning disable CS1591
namespace Wjybxx.Dson.IO;

/// <summary>
/// 简单的StringBuilder池实现，非线程安全
/// </summary>
public class SimpleBuilderPool : IStringBuilderPool
{
    private readonly Stack<StringBuilder> pool;
    private readonly int poolSize;
    private readonly int initCapacity;

    public SimpleBuilderPool(int poolSize, int initCapacity) {
        if (poolSize < 0 || initCapacity < 0) {
            throw new ArgumentException($"{nameof(poolSize)}: {poolSize}, {nameof(initCapacity)}: {initCapacity}");
        }
        this.poolSize = poolSize;
        this.initCapacity = initCapacity;
        this.pool = new Stack<StringBuilder>(poolSize);
    }

    public StringBuilder Rent() {
        if (pool.TryPop(out StringBuilder sb)) {
            return sb;
        }
        return new StringBuilder(initCapacity);
    }

    public void ReturnOne(StringBuilder sb) {
        if (sb == null) throw new ArgumentNullException(nameof(sb));
        if (pool.Count < poolSize) {
            sb.Length = 0;
            pool.Push(sb);
        }
    }
}