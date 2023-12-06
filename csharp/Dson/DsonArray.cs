/*
 * Copyright 2023 wjybxx(845740757@qq.com)
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

namespace Dson;

/// <summary>
/// 
/// </summary>
/// <typeparam name="TK">header的key类型</typeparam>
public class DsonArray<TK> : AbstractDsonArray
{
    private readonly DsonHeader<TK> _header;

    public DsonArray()
        : this(new List<DsonValue>(), new DsonHeader<TK>()) {
    }

    public DsonArray(int capacity)
        : this(new List<DsonValue>(capacity), new DsonHeader<TK>()) {
    }

    public DsonArray(DsonArray<TK> src) // 需要拷贝
        : this(new List<DsonValue>(src._values), new DsonHeader<TK>(src._header)) {
    }

    private DsonArray(IList<DsonValue> values, DsonHeader<TK> header)
        : base(values) {
        _header = header;
    }

    public override DsonType DsonType => DsonType.ARRAY;
    public DsonHeader<TK> Header => _header;

    public override DsonArray<TK> Append(DsonValue item) {
        return (DsonArray<TK>)base.Append(item);
    }
}