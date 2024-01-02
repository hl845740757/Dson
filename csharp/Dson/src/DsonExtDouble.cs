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
using Wjybxx.Dson.Types;

#pragma warning disable CS1591
namespace Wjybxx.Dson;

/// <summary>
/// Dson具有类型标签的双精度浮点数
/// </summary>
public sealed class DsonExtDouble : DsonValue, IEquatable<DsonExtDouble>, IComparable<DsonExtDouble>, IComparable
{
    private readonly ExtDouble _extDouble;

    public DsonExtDouble(int type, double? value)
        : this(new ExtDouble(type, value)) {
    }

    public DsonExtDouble(int type, double value, bool hasVal = true)
        : this(new ExtDouble(type, value, hasVal)) {
    }

    public DsonExtDouble(ExtDouble extDouble) {
        _extDouble = extDouble;
    }

    public ExtDouble ExtDouble => _extDouble;
    public override DsonType DsonType => DsonType.ExtDouble;
    public int Type => _extDouble.Type;
    public bool HasValue => _extDouble.HasValue;
    public double Value => _extDouble.Value;

    #region equals

    public bool Equals(DsonExtDouble? other) {
        if (ReferenceEquals(null, other)) return false;
        if (ReferenceEquals(this, other)) return true;
        return _extDouble.Equals(other._extDouble);
    }

    public override bool Equals(object? obj) {
        return ReferenceEquals(this, obj) || obj is DsonExtDouble other && Equals(other);
    }

    public override int GetHashCode() {
        return _extDouble.GetHashCode();
    }

    public static bool operator ==(DsonExtDouble? left, DsonExtDouble? right) {
        return Equals(left, right);
    }

    public static bool operator !=(DsonExtDouble? left, DsonExtDouble? right) {
        return !Equals(left, right);
    }

    public int CompareTo(DsonExtDouble? other) {
        if (ReferenceEquals(this, other)) return 0;
        if (ReferenceEquals(null, other)) return 1;
        return _extDouble.CompareTo(other._extDouble);
    }

    public int CompareTo(object? obj) {
        if (ReferenceEquals(null, obj)) return 1;
        if (ReferenceEquals(this, obj)) return 0;
        return obj is DsonExtDouble other ? CompareTo(other) : throw new ArgumentException($"Object must be of type {nameof(DsonExtDouble)}");
    }

    #endregion

    public override string ToString() {
        return $"{nameof(DsonType)}: {DsonType}, {nameof(_extDouble)}: {_extDouble}";
    }
}