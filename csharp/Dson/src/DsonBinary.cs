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
using System.Linq;
using Wjybxx.Dson.IO;
using Wjybxx.Dson.Types;

#pragma warning disable CS1591
namespace Wjybxx.Dson;

/// <summary>
/// Dson字节数组抽象
/// </summary>
public class DsonBinary : DsonValue, IEquatable<DsonBinary>
{
    private readonly Binary _binary;

    public DsonBinary(byte[] data)
        : this(new Binary(0, data)) {
    }

    public DsonBinary(int type, byte[] data)
        : this(new Binary(type, data)) {
    }

    public DsonBinary(int type, DsonChunk chunk)
        : this(new Binary(type, chunk.Payload())) {
    }

    public DsonBinary(Binary binary) {
        _binary = binary;
    }

    public Binary Binary => _binary;
    public override DsonType DsonType => DsonType.Binary;
    public int Type => _binary.Type;

    /// <summary>
    /// 不宜修改返回的数据
    /// </summary>
    public byte[] Data => _binary.Data;

    /// <summary>
    /// 创建一个副本
    /// </summary>
    /// <returns></returns>
    public DsonBinary Copy() {
        return new DsonBinary(_binary.Copy());
    }

    #region equals

    public bool Equals(DsonBinary? other) {
        if (ReferenceEquals(null, other)) return false;
        if (ReferenceEquals(this, other)) return true;
        return _binary.Equals(other._binary);
    }

    public override bool Equals(object? obj) {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != this.GetType()) return false;
        return Equals((DsonBinary)obj);
    }

    public override int GetHashCode() {
        return _binary.GetHashCode();
    }

    public static bool operator ==(DsonBinary? left, DsonBinary? right) {
        return Equals(left, right);
    }

    public static bool operator !=(DsonBinary? left, DsonBinary? right) {
        return !Equals(left, right);
    }

    #endregion

    public override string ToString() {
        return $"{nameof(DsonType)}: {DsonType}, {nameof(_binary)}: {_binary}";
    }
}