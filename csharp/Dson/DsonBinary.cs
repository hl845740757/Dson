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

using Dson.IO;

namespace Dson;

public class DsonBinary : DsonValue, IEquatable<DsonBinary>
{
    private readonly int _type;
    private readonly byte[] _data;

    public DsonBinary(int type, byte[] data) {
        if (data == null) throw new ArgumentNullException(nameof(data));
        Dsons.CheckSubType(type);
        Dsons.CheckBinaryLength(data.Length);
        _type = type;
        _data = data;
    }

    public DsonBinary(int type, DsonChunk chunk) {
        if (chunk == null) throw new ArgumentNullException(nameof(chunk));
        Dsons.CheckSubType(type);
        Dsons.CheckBinaryLength(chunk.Length);
        _type = type;
        _data = chunk.Payload();
    }

    public DsonBinary(DsonBinary src) {
        this._type = src._type;
        this._data = (byte[])src._data.Clone();
    }

    public override DsonType DsonType => DsonType.BINARY;
    public int Type => _type;

    /// <summary>
    /// 不宜修改返回的数据
    /// </summary>
    public byte[] Data => _data;

    /// <summary>
    /// 创建一个副本
    /// </summary>
    /// <returns></returns>
    public DsonBinary Copy() {
        return new DsonBinary(this);
    }

    #region equals

    public bool Equals(DsonBinary? other) {
        if (ReferenceEquals(null, other)) return false;
        if (ReferenceEquals(this, other)) return true;
        return _type == other._type && _data.SequenceEqual(other._data); // 比较数组元素需要使用SequenceEqual
    }

    public override bool Equals(object? obj) {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != this.GetType()) return false;
        return Equals((DsonBinary)obj);
    }

    public override int GetHashCode() {
        return HashCode.Combine(_type, _data);
    }

    public static bool operator ==(DsonBinary? left, DsonBinary? right) {
        return Equals(left, right);
    }

    public static bool operator !=(DsonBinary? left, DsonBinary? right) {
        return !Equals(left, right);
    }

    #endregion

    public override string ToString() {
        return $"{nameof(DsonType)}: {DsonType}, {nameof(_type)}: {_type}, {nameof(_data)}: {Convert.ToHexString(_data)}";
    }
}