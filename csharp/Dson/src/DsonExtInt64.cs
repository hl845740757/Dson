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
/// Dson具备类型标签的Int64类型
/// </summary>
public sealed class DsonExtInt64 : DsonValue, IEquatable<DsonExtInt64>, IComparable<DsonExtInt64>, IComparable
{
    private readonly ExtInt64 _extInt64;

    public DsonExtInt64(int type, long? value)
        : this(new ExtInt64(type, value)) {
    }

    public DsonExtInt64(int type, long value, bool hasVal = true)
        : this(new ExtInt64(type, value, hasVal)) {
    }

    public DsonExtInt64(ExtInt64 extInt64) {
        _extInt64 = extInt64;
    }

    public ExtInt64 ExtInt64 => _extInt64;
    public override DsonType DsonType => DsonType.ExtInt64;
    public int Type => _extInt64.Type;
    public bool HasValue => _extInt64.HasValue;
    public long Value => _extInt64.Value;

    #region equals

    public bool Equals(DsonExtInt64? other) {
        if (ReferenceEquals(null, other)) return false;
        if (ReferenceEquals(this, other)) return true;
        return _extInt64.Equals(other._extInt64);
    }

    public override bool Equals(object? obj) {
        return ReferenceEquals(this, obj) || obj is DsonExtInt64 other && Equals(other);
    }

    public override int GetHashCode() {
        return _extInt64.GetHashCode();
    }

    public static bool operator ==(DsonExtInt64? left, DsonExtInt64? right) {
        return Equals(left, right);
    }

    public static bool operator !=(DsonExtInt64? left, DsonExtInt64? right) {
        return !Equals(left, right);
    }

    public int CompareTo(DsonExtInt64? other) {
        if (ReferenceEquals(this, other)) return 0;
        if (ReferenceEquals(null, other)) return 1;
        return _extInt64.CompareTo(other._extInt64);
    }

    public int CompareTo(object? obj) {
        if (ReferenceEquals(null, obj)) return 1;
        if (ReferenceEquals(this, obj)) return 0;
        return obj is DsonExtInt64 other ? CompareTo(other) : throw new ArgumentException($"Object must be of type {nameof(DsonExtInt64)}");
    }

    #endregion

    public override string ToString() {
        return $"{nameof(DsonType)}: {DsonType}, {nameof(_extInt64)}: {_extInt64}";
    }
}