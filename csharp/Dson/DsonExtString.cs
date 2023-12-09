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

namespace Dson;

public class DsonExtString : DsonValue, IEquatable<DsonExtString>, IComparable<DsonExtString>, IComparable
{
    public const int MaskType = 1;
    public const int MaskValue = 1 << 1;

    private readonly int _type;
    private readonly string? _value;

    public DsonExtString(int type, string? value) {
        _type = type;
        _value = value;
    }

    public override DsonType DsonType => DsonType.ExtString;
    public int Type => _type;
    public bool HasValue => _value != null;

    /// <summary>
    /// value可能为null
    /// </summary>
    public string? Value => _value;

    #region equals

    public bool Equals(DsonExtString? other) {
        if (ReferenceEquals(null, other)) return false;
        if (ReferenceEquals(this, other)) return true;
        return _type == other._type && _value == other._value;
    }

    public override bool Equals(object? obj) {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != this.GetType()) return false;
        return Equals((DsonExtString)obj);
    }

    public override int GetHashCode() {
        return HashCode.Combine(_type, _value);
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
        var typeComparison = _type.CompareTo(other._type);
        if (typeComparison != 0) return typeComparison;
        return string.Compare(_value, other._value, StringComparison.Ordinal);
    }

    public int CompareTo(object? obj) {
        if (ReferenceEquals(null, obj)) return 1;
        if (ReferenceEquals(this, obj)) return 0;
        return obj is DsonExtString other ? CompareTo(other) : throw new ArgumentException($"Object must be of type {nameof(DsonExtString)}");
    }

    #endregion

    public override string ToString() {
        return $"{nameof(DsonType)}: {DsonType}, {nameof(_type)}: {_type}, {nameof(_value)}: {_value}";
    }
}