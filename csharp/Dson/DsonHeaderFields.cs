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

public class DsonHeader<TK> : AbstractDsonObject<TK>
{
    public DsonHeader()
        : base(DsonInternals.NewLinkedDictionary<TK>(2)) {
    }

    public DsonHeader(IDictionary<TK, DsonValue> valueMap)
        : base(DsonInternals.NewLinkedDictionary<TK>(valueMap)) {
    }

    public override DsonType DsonType => DsonType.HEADER;

    public override DsonHeader<TK> Append(TK key, DsonValue value) {
        return (DsonHeader<TK>)base.Append(key, value);
    }
}

/// <summary>
/// 定义常量
/// </summary>
public static class DsonHeaderFields
{
    // header常见属性名
    public const string NAMES_CLASS_NAME = "clsName";
    public const string NAMES_COMP_CLASS_NAME = "compClsName";
    public const string NAMES_CLASS_ID = "clsId";
    public const string NAMES_COMP_CLASS_ID = "compClsId";
    public const string NAMES_LOCAL_ID = "localId";
    public const string NAMES_TAGS = "tags";
    public const string NAMES_NAMESPACE = "ns";

    public static readonly int NUMBERS_CLASS_NAME = Dsons.MakeFullNumberZeroIdep(0);
    public static readonly int NUMBERS_COMP_CLASS_NAME = Dsons.MakeFullNumberZeroIdep(1);
    public static readonly int NUMBERS_CLASS_ID = Dsons.MakeFullNumberZeroIdep(2);
    public static readonly int NUMBERS_COMP_CLASS_ID = Dsons.MakeFullNumberZeroIdep(3);
    public static readonly int NUMBERS_LOCAL_ID = Dsons.MakeFullNumberZeroIdep(4);
    public static readonly int NUMBERS_TAGS = Dsons.MakeFullNumberZeroIdep(5);
    public static readonly int NUMBERS_NAMESPACE = Dsons.MakeFullNumberZeroIdep(6);
}