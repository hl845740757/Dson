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

using System.Collections.Generic;

#pragma warning disable CS1591
namespace Wjybxx.Dson;

/// <summary>
/// Dson对象头
/// </summary>
/// <typeparam name="TK">String或<see cref="FieldNumber"/></typeparam>
public class DsonHeader<TK> : AbstractDsonObject<TK>
{
    public DsonHeader()
        : base(DsonInternals.NewLinkedDictionary<TK>(2)) {
    }

    public DsonHeader(IDictionary<TK, DsonValue> valueMap)
        : base(DsonInternals.NewLinkedDictionary(valueMap)) {
    }

    public override DsonType DsonType => DsonType.Header;

    public override DsonHeader<TK> Append(TK key, DsonValue value) {
        return (DsonHeader<TK>)base.Append(key, value);
    }
}

/// <summary>
/// 定义DsonHeader常量
/// </summary>
public static class DsonHeaders
{
    // header常见属性名
    public const string NamesClassName = "clsName";
    public const string NamesCompClassName = "compClsName";
    public const string NamesClassId = "clsId";
    public const string NamesCompClassId = "compClsId";
    public const string NamesLocalId = "localId";
    public const string NamesTags = "tags";
    public const string NamesNamespace = "ns";

    public static readonly FieldNumber NumbersClassName = FieldNumber.OfLnumber(0);
    public static readonly FieldNumber NumbersCompClassName = FieldNumber.OfLnumber(1);
    public static readonly FieldNumber NumbersClassId = FieldNumber.OfLnumber(2);
    public static readonly FieldNumber NumbersCompClassId = FieldNumber.OfLnumber(3);
    public static readonly FieldNumber NumbersLocalId = FieldNumber.OfLnumber(4);
    public static readonly FieldNumber NumbersTags = FieldNumber.OfLnumber(5);
    public static readonly FieldNumber NumbersNamespace = FieldNumber.OfLnumber(6);
}