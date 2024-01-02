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
using System.Linq;

#pragma warning disable CS1591
namespace Wjybxx.Dson.Types;

/// <summary>
/// 具有类型标签的二进制数据(字节数组)
/// </summary>
public readonly struct Binary : IEquatable<Binary>
{
    private readonly int _type;
    private readonly byte[] _data;

    public Binary(byte[] data)
        : this(0, data) {
    }

    public Binary(int type, byte[] data) {
        if (data == null) throw new ArgumentNullException(nameof(data));
        Dsons.CheckSubType(type);
        Dsons.CheckBinaryLength(data.Length);
        _type = type;
        _data = data;
    }

    public Binary(Binary src) {
        this._type = src._type;
        this._data = (byte[])src._data.Clone();
    }

    public int Type => _type;

    /// <summary>
    /// 不宜修改返回的数据
    /// </summary>
    public byte[] Data => _data;

    public Binary Copy() {
        return new Binary(_type, (byte[])_data.Clone());
    }

    #region equals

    public bool Equals(Binary other) {
        return _type == other._type && _data.SequenceEqual(other._data);
    }

    public override bool Equals(object? obj) {
        return obj is Binary other && Equals(other);
    }

    public override int GetHashCode() {
        return HashCode.Combine(_type, _data);
    }

    #endregion

    public override string ToString() {
        return $"{nameof(_type)}: {_type}, {nameof(_data)}: {Convert.ToHexString(_data)}";
    }
}