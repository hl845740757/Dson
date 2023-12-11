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

public class DsonBool : DsonValue, IComparable<DsonBool>, IEquatable<DsonBool>, IComparable
{
    public static readonly DsonBool True = new DsonBool(true);
    public static readonly DsonBool False = new DsonBool(false);

    private readonly bool _value;

    public DsonBool(bool value) {
        _value = value;
    }

    public static DsonBool ValueOf(bool value) {
        return value ? True : False;
    }

    public override DsonType DsonType => DsonType.Boolean;
    public bool Value => _value;

    #region equals

    public bool Equals(DsonBool? other) {
        if (ReferenceEquals(null, other)) return false;
        if (ReferenceEquals(this, other)) return true;
        return _value == other._value;
    }

    public override bool Equals(object? obj) {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != this.GetType()) return false;
        return Equals((DsonBool)obj);
    }

    public override int GetHashCode() {
        return _value.GetHashCode();
    }

    public static bool operator ==(DsonBool? left, DsonBool? right) {
        return Equals(left, right);
    }

    public static bool operator !=(DsonBool? left, DsonBool? right) {
        return !Equals(left, right);
    }

    public int CompareTo(DsonBool? other) {
        if (ReferenceEquals(this, other)) return 0;
        if (ReferenceEquals(null, other)) return 1;
        return _value.CompareTo(other._value);
    }

    public int CompareTo(object? obj) {
        if (ReferenceEquals(null, obj)) return 1;
        if (ReferenceEquals(this, obj)) return 0;
        return obj is DsonBool other ? CompareTo(other) : throw new ArgumentException($"Object must be of type {nameof(DsonBool)}");
    }

    public static bool operator <(DsonBool? left, DsonBool? right) {
        return Comparer<DsonBool>.Default.Compare(left, right) < 0;
    }

    public static bool operator >(DsonBool? left, DsonBool? right) {
        return Comparer<DsonBool>.Default.Compare(left, right) > 0;
    }

    public static bool operator <=(DsonBool? left, DsonBool? right) {
        return Comparer<DsonBool>.Default.Compare(left, right) <= 0;
    }

    public static bool operator >=(DsonBool? left, DsonBool? right) {
        return Comparer<DsonBool>.Default.Compare(left, right) >= 0;
    }

    #endregion

    public override string ToString() {
        return $"{nameof(DsonType)}: {DsonType}, {nameof(_value)}: {_value}";
    }
}