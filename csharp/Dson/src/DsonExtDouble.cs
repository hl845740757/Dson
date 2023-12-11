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

namespace Wjybxx.Dson;

public class DsonExtDouble : DsonValue, IEquatable<DsonExtDouble>, IComparable<DsonExtDouble>, IComparable
{
    private readonly int _type;
    private readonly bool _hasVal; // 比较时放前面
    private readonly double _value;

    public DsonExtDouble(int type, double? value)
        : this(type, value ?? 0, value.HasValue) {
    }

    public DsonExtDouble(int type, double value, bool hasVal = true) {
        Dsons.CheckSubType(type);
        Dsons.CheckHasValue(value, hasVal);
        _type = type;
        _value = value;
        _hasVal = hasVal;
    }

    public override DsonType DsonType => DsonType.ExtDouble;
    public int Type => _type;
    public bool HasValue => _hasVal;
    public double Value => _value;

    #region equals

    public bool Equals(DsonExtDouble? other) {
        if (ReferenceEquals(null, other)) return false;
        if (ReferenceEquals(this, other)) return true;
        return _type == other._type && _value.Equals(other._value) && _hasVal == other._hasVal;
    }

    public override bool Equals(object? obj) {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != this.GetType()) return false;
        return Equals((DsonExtDouble)obj);
    }

    public override int GetHashCode() {
        return HashCode.Combine(_type, _value, _hasVal);
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
        var typeComparison = _type.CompareTo(other._type);
        if (typeComparison != 0) return typeComparison;
        var hasValComparison = _hasVal.CompareTo(other._hasVal);
        if (hasValComparison != 0) return hasValComparison;
        return _value.CompareTo(other._value);
    }

    public int CompareTo(object? obj) {
        if (ReferenceEquals(null, obj)) return 1;
        if (ReferenceEquals(this, obj)) return 0;
        return obj is DsonExtDouble other ? CompareTo(other) : throw new ArgumentException($"Object must be of type {nameof(DsonExtDouble)}");
    }

    public static bool operator <(DsonExtDouble? left, DsonExtDouble? right) {
        return Comparer<DsonExtDouble>.Default.Compare(left, right) < 0;
    }

    public static bool operator >(DsonExtDouble? left, DsonExtDouble? right) {
        return Comparer<DsonExtDouble>.Default.Compare(left, right) > 0;
    }

    public static bool operator <=(DsonExtDouble? left, DsonExtDouble? right) {
        return Comparer<DsonExtDouble>.Default.Compare(left, right) <= 0;
    }

    public static bool operator >=(DsonExtDouble? left, DsonExtDouble? right) {
        return Comparer<DsonExtDouble>.Default.Compare(left, right) >= 0;
    }

    #endregion

    public override string ToString() {
        return $"{nameof(DsonType)}: {DsonType}, {nameof(_type)}: {_type}, {nameof(_hasVal)}: {_hasVal}, {nameof(_value)}: {_value}";
    }
}