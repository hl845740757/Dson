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
/// Dson具备类型标签的Int32类型
/// </summary>
public sealed class DsonExtInt32 : DsonValue, IEquatable<DsonExtInt32>, IComparable<DsonExtInt32>, IComparable
{
    private readonly ExtInt32 _extInt32;

    public DsonExtInt32(int type, int? value)
        : this(new ExtInt32(type, value)) {
    }

    public DsonExtInt32(int type, int value, bool hasVal = true)
        : this(new ExtInt32(type, value, hasVal)) {
    }

    public DsonExtInt32(ExtInt32 extInt32) {
        _extInt32 = extInt32;
    }

    public ExtInt32 ExtInt32 => _extInt32;
    public override DsonType DsonType => DsonType.ExtInt32;
    public int Type => _extInt32.Type;
    public bool HasValue => _extInt32.HasValue;
    public int Value => _extInt32.Value;

    #region equals

    public bool Equals(DsonExtInt32? other) {
        if (ReferenceEquals(null, other)) return false;
        if (ReferenceEquals(this, other)) return true;
        return _extInt32.Equals(other._extInt32);
    }

    public override bool Equals(object? obj) {
        return ReferenceEquals(this, obj) || obj is DsonExtInt32 other && Equals(other);
    }

    public override int GetHashCode() {
        return _extInt32.GetHashCode();
    }

    public static bool operator ==(DsonExtInt32? left, DsonExtInt32? right) {
        return Equals(left, right);
    }

    public static bool operator !=(DsonExtInt32? left, DsonExtInt32? right) {
        return !Equals(left, right);
    }

    public int CompareTo(DsonExtInt32? other) {
        if (ReferenceEquals(this, other)) return 0;
        if (ReferenceEquals(null, other)) return 1;
        return _extInt32.CompareTo(other._extInt32);
    }

    public int CompareTo(object? obj) {
        if (ReferenceEquals(null, obj)) return 1;
        if (ReferenceEquals(this, obj)) return 0;
        return obj is DsonExtInt32 other ? CompareTo(other) : throw new ArgumentException($"Object must be of type {nameof(DsonExtInt32)}");
    }

    #endregion

    public override string ToString() {
        return $"{nameof(DsonType)}: {DsonType}, {nameof(_extInt32)}: {_extInt32}";
    }
}