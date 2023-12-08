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

public class DsonExtInt64 : DsonValue, IEquatable<DsonExtInt64>, IComparable<DsonExtInt64>, IComparable
{
    private readonly int _type;
    private readonly bool _hasVal; // 比较时放前面
    private readonly long _value;

    public DsonExtInt64(int type, long? value)
        : this(type, value ?? 0, value.HasValue) {
    }

    public DsonExtInt64(int type, long value, bool hasVal = true) {
        Dsons.CheckSubType(type);
        Dsons.CheckHasValue(value, hasVal);
        _type = type;
        _value = value;
        _hasVal = hasVal;
    }

    public override DsonType DsonType => DsonType.EXT_INT64;
    public int Type => _type;
    public bool HasValue => _hasVal;
    public long Value => _value;

    #region equals

    public bool Equals(DsonExtInt64? other) {
        if (ReferenceEquals(null, other)) return false;
        if (ReferenceEquals(this, other)) return true;
        return _type == other._type && _value == other._value && _hasVal == other._hasVal;
    }

    public override bool Equals(object? obj) {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != this.GetType()) return false;
        return Equals((DsonExtInt64)obj);
    }

    public override int GetHashCode() {
        return HashCode.Combine(_type, _value, _hasVal);
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
        var typeComparison = _type.CompareTo(other._type);
        if (typeComparison != 0) return typeComparison;
        var hasValComparison = _hasVal.CompareTo(other._hasVal);
        if (hasValComparison != 0) return hasValComparison;
        return _value.CompareTo(other._value);
    }

    public int CompareTo(object? obj) {
        if (ReferenceEquals(null, obj)) return 1;
        if (ReferenceEquals(this, obj)) return 0;
        return obj is DsonExtInt64 other ? CompareTo(other) : throw new ArgumentException($"Object must be of type {nameof(DsonExtInt64)}");
    }

    public static bool operator <(DsonExtInt64? left, DsonExtInt64? right) {
        return Comparer<DsonExtInt64>.Default.Compare(left, right) < 0;
    }

    public static bool operator >(DsonExtInt64? left, DsonExtInt64? right) {
        return Comparer<DsonExtInt64>.Default.Compare(left, right) > 0;
    }

    public static bool operator <=(DsonExtInt64? left, DsonExtInt64? right) {
        return Comparer<DsonExtInt64>.Default.Compare(left, right) <= 0;
    }

    public static bool operator >=(DsonExtInt64? left, DsonExtInt64? right) {
        return Comparer<DsonExtInt64>.Default.Compare(left, right) >= 0;
    }

    #endregion

    public override string ToString() {
        return $"{nameof(DsonType)}: {DsonType}, {nameof(_type)}: {_type}, {nameof(_hasVal)}: {_hasVal}, {nameof(_value)}: {_value}";
    }
}