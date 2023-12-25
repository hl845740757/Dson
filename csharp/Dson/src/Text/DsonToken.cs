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

#pragma warning disable CS1591
namespace Wjybxx.Dson.Text;

/// <summary>
/// Dson文本token
/// (值类型小心使用)
/// </summary>
public readonly struct DsonToken : IEquatable<DsonToken>
{
#nullable disable
    /** token的类型 */
    public readonly DsonTokenType Type;
    /** token关联的值 */
    public readonly object Value;
    /** token所在的位置，-1表示动态生成的token */
    public readonly int Pos;
#nullable enable

    public DsonToken(DsonTokenType type, object? value, int pos) {
        this.Type = type;
        this.Value = value;
        this.Pos = pos;
    }

    /** 将value转换为字符串值 */
    public string CastAsString() {
        return (string)Value!;
    }

    /** 获取字符串value的第一个char */
    public char FirstChar() {
        var value = (string)this.Value!;
        return value[0];
    }

    /** 获取字符串value的最后一个char */
    public char LastChar() {
        string value = (string)this.Value!;
        return value[value.Length - 1];
    }

    #region equals

    // Equals默认不比较位置

    public bool Equals(DsonToken other) {
        return Type == other.Type && Equals(Value, other.Value);
    }

    public override bool Equals(object? obj) {
        return obj is DsonToken other && Equals(other);
    }

    public override int GetHashCode() {
        return HashCode.Combine((int)Type, Value);
    }

    public static bool operator ==(DsonToken left, DsonToken right) {
        return left.Equals(right);
    }

    public static bool operator !=(DsonToken left, DsonToken right) {
        return !left.Equals(right);
    }

    #endregion

    public override string ToString() {
        return $"{nameof(Type)}: {Type}, {nameof(Value)}: {Value}, {nameof(Pos)}: {Pos}";
    }
}