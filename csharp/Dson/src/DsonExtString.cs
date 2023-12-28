#region LICENSE

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
using Wjybxx.Dson.Types;

#pragma warning disable CS1591
namespace Wjybxx.Dson;

/// <summary>
/// Dson具备类型标签的String，支持value为null
/// </summary>
public sealed class DsonExtString : DsonValue, IEquatable<DsonExtString>, IComparable<DsonExtString>, IComparable
{
    private readonly ExtString _extString;

    public DsonExtString(int type, string? value)
        : this(new ExtString(type, value)) {
    }

    public DsonExtString(ExtString extString) {
        _extString = extString;
    }

    public ExtString ExtString => _extString;
    public override DsonType DsonType => DsonType.ExtString;
    public int Type => _extString.Type;
    public bool HasValue => _extString.HasValue;

    /// <summary>
    /// value可能为null
    /// </summary>
    public string? Value => _extString.Value;

    #region equals

    public bool Equals(DsonExtString? other) {
        if (ReferenceEquals(null, other)) return false;
        if (ReferenceEquals(this, other)) return true;
        return _extString.Equals(other._extString);
    }

    public override bool Equals(object? obj) {
        return ReferenceEquals(this, obj) || obj is DsonExtString other && Equals(other);
    }

    public override int GetHashCode() {
        return _extString.GetHashCode();
    }

    public static bool operator ==(DsonExtString? left, DsonExtString? right) {
        return Equals(left, right);
    }

    public static bool operator !=(DsonExtString? left, DsonExtString? right) {
        return !Equals(left, right);
    }

    public int CompareTo(DsonExtString? other) {
        if (ReferenceEquals(this, other)) return 0;
        if (ReferenceEquals(null, other)) return 1;
        return _extString.CompareTo(other._extString);
    }

    public int CompareTo(object? obj) {
        if (ReferenceEquals(null, obj)) return 1;
        if (ReferenceEquals(this, obj)) return 0;
        return obj is DsonExtString other ? CompareTo(other) : throw new ArgumentException($"Object must be of type {nameof(DsonExtString)}");
    }

    #endregion

    public override string ToString() {
        return $"{nameof(DsonType)}: {DsonType}, {nameof(_extString)}: {_extString}";
    }
}