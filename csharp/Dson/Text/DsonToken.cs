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

namespace Dson.Text;

public class DsonToken : IEquatable<DsonToken>
{
#nullable disable
    public readonly DsonTokenType Type;
    public readonly object Value;
    public readonly int Pos;
#nullable enable
    /// <summary>
    /// 
    /// </summary>
    /// <param name="type">token的类型</param>
    /// <param name="value">token关联的值</param>
    /// <param name="pos">pos token所在的位置，-1表示动态生成的token</param>
    public DsonToken(DsonTokenType type, object? value, int pos) {
        this.Type = type;
        this.Value = value;
        this.Pos = pos;
    }

    public string castAsString() {
        return (string)Value!;
    }

    public char firstChar() {
        String value = (String)this.Value!;
        return value[0];
    }

    public char lastChar() {
        string value = (string)this.Value!;
        return value[value.Length - 1];
    }

    #region equals

    // Equals默认不比较位置

    public bool Equals(DsonToken? other) {
        if (ReferenceEquals(null, other)) return false;
        if (ReferenceEquals(this, other)) return true;
        return Type == other.Type && Equals(Value, other.Value);
    }

    public override bool Equals(object? obj) {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != this.GetType()) return false;
        return Equals((DsonToken)obj);
    }

    public override int GetHashCode() {
        return HashCode.Combine((int)Type, Value);
    }

    public static bool operator ==(DsonToken? left, DsonToken? right) {
        return Equals(left, right);
    }

    public static bool operator !=(DsonToken? left, DsonToken? right) {
        return !Equals(left, right);
    }

    #endregion

    public override string ToString() {
        return $"{nameof(Type)}: {Type}, {nameof(Value)}: {Value}, {nameof(Pos)}: {Pos}";
    }
}