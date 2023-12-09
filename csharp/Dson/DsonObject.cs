#region LICENSE

//  Copyright 2023 wjybxx
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

namespace Wjybxx.Dson;

/// <summary>
/// 
/// </summary>
/// <typeparam name="TK">String或<see cref="FieldNumber"/></typeparam>
public class DsonObject<TK> : AbstractDsonObject<TK>
{
    private readonly DsonHeader<TK> _header;

    public DsonObject()
        : this(DsonInternals.NewLinkedDictionary<TK>(), new DsonHeader<TK>()) {
    }

    public DsonObject(DsonObject<TK> src) // 需要拷贝
        : this(DsonInternals.NewLinkedDictionary(src), new DsonHeader<TK>(src._header)) {
    }

    private DsonObject(IDictionary<TK, DsonValue> valueMap, DsonHeader<TK> header)
        : base(valueMap) {
        _header = header;
    }

    public override DsonType DsonType => DsonType.Object;
    public DsonHeader<TK> Header => _header;

    public override DsonObject<TK> Append(TK key, DsonValue value) {
        return (DsonObject<TK>)base.Append(key, value);
    }
}