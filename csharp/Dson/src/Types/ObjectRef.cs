﻿#region LICENSE

//  Copyright 2023 wjybxx(845740757@qq.com)
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

#pragma warning disable CS1591
namespace Wjybxx.Dson.Types;

/// <summary>
/// 对象引用
/// </summary>
public readonly struct ObjectRef : IEquatable<ObjectRef>
{
    public const int MaskNamespace = 1;
    public const int MaskType = 1 << 1;
    public const int MaskPolicy = 1 << 2;

    /** 引用对象的本地id - 如果目标对象是容器中的一员，该值是其容器内编号 */
    public readonly string LocalId;
    /** 引用对象所属的命名空间 -- namespace是关键字，这里缩写 */
    public readonly string Ns;
    /** 引用的对象的大类型 - 给业务使用的，用于快速引用分析 */
    public readonly int Type;
    /** 引用的解析策略 - 0：默认 1：解析为引用 2：内联复制，3：不解析 */
    public readonly int Policy;

    public ObjectRef(string? localId, string? ns = null, int type = 0, int policy = 0) {
        this.LocalId = localId ?? "";
        this.Ns = ns ?? "";
        this.Type = type;
        this.Policy = policy;
    }

    public bool IsEmpty => string.IsNullOrWhiteSpace(LocalId) && string.IsNullOrWhiteSpace(Ns);

    public bool hasLocalId => !string.IsNullOrWhiteSpace(LocalId);

    public bool hasNamespace => !string.IsNullOrWhiteSpace(Ns);

    #region equals

    public bool Equals(ObjectRef other) {
        return LocalId == other.LocalId && Ns == other.Ns && Type == other.Type && Policy == other.Policy;
    }

    public override bool Equals(object? obj) {
        return obj is ObjectRef other && Equals(other);
    }

    public override int GetHashCode() {
        return HashCode.Combine(LocalId, Ns, Type, Policy);
    }

    public static bool operator ==(ObjectRef left, ObjectRef right) {
        return left.Equals(right);
    }

    public static bool operator !=(ObjectRef left, ObjectRef right) {
        return !left.Equals(right);
    }

    #endregion

    public override string ToString() {
        return $"{nameof(LocalId)}: {LocalId}, {nameof(Ns)}: {Ns}, {nameof(Type)}: {Type}, {nameof(Policy)}: {Policy}";
    }

    #region 常量

    public const string NamesNamespace = "ns";
    public const string NamesLocalId = "localId";
    public const string NamesType = "type";
    public const string NamesPolicy = "policy";

    public static readonly FieldNumber NumbersNamespace = FieldNumber.OfLnumber(0);
    public static readonly FieldNumber NumbersLocalId = FieldNumber.OfLnumber(1);
    public static readonly FieldNumber NumbersType = FieldNumber.OfLnumber(2);
    public static readonly FieldNumber NumbersPolicy = FieldNumber.OfLnumber(3);

    #endregion
}