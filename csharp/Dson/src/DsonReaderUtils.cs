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
using System.Collections.Generic;
using System.Collections.Immutable;
using Wjybxx.Dson.IO;
using Wjybxx.Dson.Types;

#pragma warning disable CS1591
namespace Wjybxx.Dson;

/// <summary>
/// Dson二进制编解码工具类
/// </summary>
public static class DsonReaderUtils
{
    /** 支持读取为bytes和直接写入bytes的数据类型 -- 这些类型不可以存储额外数据在WireType上 */
    public static readonly IList<DsonType> ValueBytesTypes = new[]
    {
        DsonType.String, DsonType.Binary, DsonType.Array, DsonType.Object, DsonType.Header
    }.ToImmutableList();

    #region number

    public static void WriteInt32(IDsonOutput output, int value, WireType wireType) {
        switch (wireType) {
            case WireType.VarInt: {
                output.WriteInt32(value);
                break;
            }
            case WireType.Uint: {
                output.WriteUint32(value);
                break;
            }
            case WireType.Sint: {
                output.WriteSint32(value);
                break;
            }
            case WireType.Fixed: {
                output.WriteFixed32(value);
                break;
            }
            default:
                throw new ArgumentOutOfRangeException(nameof(wireType), wireType, null);
        }
    }

    public static int ReadInt32(IDsonInput input, WireType wireType) {
        switch (wireType) {
            case WireType.VarInt: {
                return input.ReadInt32();
            }
            case WireType.Uint: {
                return input.ReadUint32();
            }
            case WireType.Sint: {
                return input.ReadSint32();
            }
            case WireType.Fixed: {
                return input.ReadFixed32();
            }
            default:
                throw new ArgumentOutOfRangeException(nameof(wireType), wireType, null);
        }
    }

    public static void WriteInt64(IDsonOutput output, long value, WireType wireType) {
        switch (wireType) {
            case WireType.VarInt: {
                output.WriteInt64(value);
                break;
            }
            case WireType.Uint: {
                output.WriteUint64(value);
                break;
            }
            case WireType.Sint: {
                output.WriteSint64(value);
                break;
            }
            case WireType.Fixed: {
                output.WriteFixed64(value);
                break;
            }
            default:
                throw new ArgumentOutOfRangeException(nameof(wireType), wireType, null);
        }
    }

    public static long ReadInt64(IDsonInput input, WireType wireType) {
        switch (wireType) {
            case WireType.VarInt: {
                return input.ReadInt64();
            }
            case WireType.Uint: {
                return input.ReadUint64();
            }
            case WireType.Sint: {
                return input.ReadSint64();
            }
            case WireType.Fixed: {
                return input.ReadFixed64();
            }
            default:
                throw new ArgumentOutOfRangeException(nameof(wireType), wireType, null);
        }
    }

    /**
     * 1.浮点数的前16位固定写入，因此只统计后16位
     * 2.wireType表示后导0对应的字节数
     * 3.由于编码依赖了上层的wireType比特位，因此不能写在Output接口中
     */
    public static int WireTypeOfFloat(float value) {
        int rawBits = BitConverter.SingleToInt32Bits(value);
        if ((rawBits & 0xFF) != 0) {
            return 0;
        }
        if ((rawBits & 0xFF00) != 0) {
            return 1;
        }
        return 2;
    }

    /** 小端编码，从末尾非0开始写入 */
    public static void WriteFloat(IDsonOutput output, float value, int wireType) {
        if (wireType == 0) {
            output.WriteFloat(value);
            return;
        }

        int rawBits = BitConverter.SingleToInt32Bits(value);
        for (int i = 0; i < wireType; i++) {
            rawBits = rawBits >> 8;
        }
        for (int i = wireType; i < 4; i++) {
            output.WriteRawByte((byte)rawBits);
            rawBits = rawBits >> 8;
        }
    }

    public static float ReadFloat(IDsonInput input, int wireType) {
        if (wireType == 0) {
            return input.ReadFloat();
        }

        int rawBits = 0;
        for (int i = wireType; i < 4; i++) {
            rawBits |= (input.ReadRawByte() & 0XFF) << (8 * i);
        }
        return BitConverter.Int32BitsToSingle(rawBits);
    }

    /**
     * 浮点数的前16位固定写入，因此只统计后48位
     */
    public static int WireTypeOfDouble(double value) {
        long rawBits = BitConverter.DoubleToInt64Bits(value);
        for (int i = 0; i < 6; i++) {
            byte v = (byte)rawBits;
            if (v != 0) {
                return i;
            }
            rawBits = rawBits >> 8;
        }
        return 6;
    }

    public static void WriteDouble(IDsonOutput output, double value, int wireType) {
        if (wireType == 0) {
            output.WriteDouble(value);
            return;
        }

        long rawBits = BitConverter.DoubleToInt64Bits(value);
        for (int i = 0; i < wireType; i++) {
            rawBits = rawBits >> 8;
        }
        for (int i = wireType; i < 8; i++) {
            output.WriteRawByte((byte)rawBits);
            rawBits = rawBits >> 8;
        }
    }

    public static double ReadDouble(IDsonInput input, int wireType) {
        if (wireType == 0) {
            return input.ReadDouble();
        }

        long rawBits = 0;
        for (int i = wireType; i < 8; i++) {
            rawBits |= (input.ReadRawByte() & 0XFFL) << (8 * i);
        }
        return BitConverter.Int64BitsToDouble(rawBits);
    }

    public static bool ReadBool(IDsonInput input, int wireTypeBits) {
        if (wireTypeBits == 1) {
            return true;
        }
        if (wireTypeBits == 0) {
            return false;
        }
        throw new DsonIOException("invalid wireType for bool, bits: " + wireTypeBits);
    }

    #endregion

    #region binary

    public static void WriteBinary(IDsonOutput output, Binary binary) {
        int sizeOfBinaryType = BinaryUtils.ComputeRawVarInt32Size((uint)binary.Type);
        output.WriteFixed32(sizeOfBinaryType + binary.Data.Length);
        output.WriteUint32(binary.Type);
        output.WriteRawBytes(binary.Data);
    }

    public static void WriteBinary(IDsonOutput output, int type, DsonChunk chunk) {
        int sizeOfBinaryType = BinaryUtils.ComputeRawVarInt32Size((uint)type);
        output.WriteFixed32(sizeOfBinaryType + chunk.Length);
        output.WriteUint32(type);
        output.WriteRawBytes(chunk.Buffer, chunk.Offset, chunk.Length);
    }

    public static Binary ReadBinary(IDsonInput input) {
        int size = input.ReadFixed32();
        int oldLimit = input.PushLimit(size);
        Binary binary;
        {
            int type = input.ReadUint32();
            int dataLength = input.GetBytesUntilLimit();
            binary = new Binary(type, input.ReadRawBytes(dataLength));
        }
        input.PopLimit(oldLimit);
        return binary;
    }

    #endregion

    #region 内置二元组

    public static void WriteExtInt32(IDsonOutput output, ExtInt32 extInt32, WireType wireType) {
        output.WriteUint32(extInt32.Type);
        output.WriteBool(extInt32.HasValue);
        if (extInt32.HasValue) {
            WriteInt32(output, extInt32.Value, wireType);
        }
    }

    public static ExtInt32 ReadExtInt32(IDsonInput input, WireType wireType) {
        int type = input.ReadUint32();
        bool hasValue = input.ReadBool();
        int value = hasValue ? ReadInt32(input, wireType) : 0;
        return new ExtInt32(type, value, hasValue);
    }

    public static void WriteExtInt64(IDsonOutput output, ExtInt64 extInt64, WireType wireType) {
        output.WriteUint32(extInt64.Type);
        output.WriteBool(extInt64.HasValue);
        if (extInt64.HasValue) {
            WriteInt64(output, extInt64.Value, wireType);
        }
    }

    public static ExtInt64 ReadExtInt64(IDsonInput input, WireType wireType) {
        int type = input.ReadUint32();
        bool hasValue = input.ReadBool();
        long value = hasValue ? ReadInt64(input, wireType) : 0;
        return new ExtInt64(type, value, hasValue);
    }

    public static void WriteExtDouble(IDsonOutput output, ExtDouble extDouble, int wireType) {
        output.WriteUint32(extDouble.Type);
        output.WriteBool(extDouble.HasValue);
        if (extDouble.HasValue) {
            WriteDouble(output, extDouble.Value, wireType);
        }
    }

    public static ExtDouble ReadExtDouble(IDsonInput input, int wireType) {
        int type = input.ReadUint32();
        bool hasValue = input.ReadBool();
        double value = hasValue ? ReadDouble(input, wireType) : 0;
        return new ExtDouble(type, value, hasValue);
    }

    public static int WireTypeOfExtString(ExtString extString) {
        int v = 0;
        if (extString.Type != 0) {
            v |= ExtString.MaskType;
        }
        if (extString.HasValue) {
            v |= ExtString.MaskValue;
        }
        return v;
    }

    public static void WriteExtString(IDsonOutput output, ExtString extString) {
        if (extString.Type != 0) {
            output.WriteUint32(extString.Type);
        }
        if (extString.HasValue) {
            output.WriteString(extString.Value!);
        }
    }

    public static ExtString ReadExtString(IDsonInput input, int wireTypeBits) {
        int type = DsonInternals.IsEnabled(wireTypeBits, ExtString.MaskType) ? input.ReadUint32() : 0;
        string value = DsonInternals.IsEnabled(wireTypeBits, ExtString.MaskValue) ? input.ReadString() : null;
        return new ExtString(type, value);
    }

    #endregion

    #region 内置结构体

    public static int WireTypeOfRef(ObjectRef objectRef) {
        int v = 0;
        if (objectRef.hasNamespace) {
            v |= ObjectRef.MaskNamespace;
        }
        if (objectRef.Type != 0) {
            v |= ObjectRef.MaskType;
        }
        if (objectRef.Policy != 0) {
            v |= ObjectRef.MaskPolicy;
        }
        return v;
    }

    public static void WriteRef(IDsonOutput output, ObjectRef objectRef) {
        output.WriteString(objectRef.LocalId);
        if (objectRef.hasNamespace) {
            output.WriteString(objectRef.Ns);
        }
        if (objectRef.Type != 0) {
            output.WriteUint32(objectRef.Type);
        }
        if (objectRef.Policy != 0) {
            output.WriteUint32(objectRef.Policy);
        }
    }

    public static ObjectRef ReadRef(IDsonInput input, int wireTypeBits) {
        string localId = input.ReadString();
        string ns = DsonInternals.IsEnabled(wireTypeBits, ObjectRef.MaskNamespace) ? input.ReadString() : null;
        int type = DsonInternals.IsEnabled(wireTypeBits, ObjectRef.MaskType) ? input.ReadUint32() : 0;
        int policy = DsonInternals.IsEnabled(wireTypeBits, ObjectRef.MaskPolicy) ? input.ReadUint32() : 0;
        return new ObjectRef(localId, ns, type, policy);
    }

    public static void WriteTimestamp(IDsonOutput output, OffsetTimestamp timestamp) {
        output.WriteUint64(timestamp.Seconds);
        output.WriteUint32(timestamp.Nanos);
        output.WriteSint32(timestamp.Offset);
        output.WriteUint32(timestamp.Enables);
    }

    public static OffsetTimestamp ReadTimestamp(IDsonInput input) {
        return new OffsetTimestamp(
            input.ReadUint64(),
            input.ReadUint32(),
            input.ReadSint32(),
            input.ReadUint32());
    }

    #endregion

    #region 特殊

    public static void WriteValueBytes(IDsonOutput output, DsonType type, byte[] data) {
        if (type == DsonType.String) {
            output.WriteUint32(data.Length);
        } else {
            output.WriteFixed32(data.Length);
        }
        output.WriteRawBytes(data);
    }

    public static byte[] ReadValueAsBytes(IDsonInput input, DsonType dsonType) {
        int size;
        if (dsonType == DsonType.String) {
            size = input.ReadUint32();
        } else {
            size = input.ReadFixed32();
        }
        return input.ReadRawBytes(size);
    }

    public static void CheckReadValueAsBytes(DsonType dsonType) {
        if (!ValueBytesTypes.Contains(dsonType)) {
            throw DsonIOException.InvalidDsonType(ValueBytesTypes, dsonType);
        }
    }

    public static void CheckWriteValueAsBytes(DsonType dsonType) {
        if (!ValueBytesTypes.Contains(dsonType)) {
            throw DsonIOException.InvalidDsonType(ValueBytesTypes, dsonType);
        }
    }

    public static void SkipToEndOfObject(IDsonInput input) {
        int size = input.GetBytesUntilLimit();
        if (size > 0) {
            input.SkipRawBytes(size);
        }
    }

    #endregion

    public static void SkipValue(IDsonInput input, DsonContextType contextType,
                                 DsonType dsonType, WireType wireType, int wireTypeBits) {
        int skip;
        switch (dsonType) {
            case DsonType.Float: {
                skip = 4 - wireTypeBits;
                break;
            }
            case DsonType.Double: {
                skip = 8 - wireTypeBits;
                break;
            }
            case DsonType.Boolean:
            case DsonType.Null: {
                return;
            }
            case DsonType.Int32: {
                ReadInt32(input, wireType);
                return;
            }
            case DsonType.Int64: {
                ReadInt64(input, wireType);
                return;
            }
            case DsonType.String: {
                skip = input.ReadUint32(); // string长度
                break;
            }
            case DsonType.ExtInt32: {
                input.ReadUint32(); // 子类型
                if (input.ReadBool()) {
                    ReadInt32(input, wireType);
                }
                return;
            }
            case DsonType.ExtInt64: {
                input.ReadUint32(); // 子类型
                if (input.ReadBool()) {
                    ReadInt64(input, wireType);
                }
                return;
            }
            case DsonType.ExtDouble: {
                input.ReadUint32(); // 子类型
                skip = input.ReadBool() ? (8 - wireTypeBits) : 0;
                input.SkipRawBytes(skip);
                return;
            }
            case DsonType.ExtString: {
                if (DsonInternals.IsEnabled(wireTypeBits, ExtString.MaskType)) {
                    input.ReadUint32(); // 子类型
                }
                if (DsonInternals.IsEnabled(wireTypeBits, ExtString.MaskValue)) {
                    skip = input.ReadUint32(); // string长度
                    input.SkipRawBytes(skip);
                }
                return;
            }
            case DsonType.Reference: {
                skip = input.ReadUint32(); // localId
                input.SkipRawBytes(skip);

                if (DsonInternals.IsEnabled(wireTypeBits, ObjectRef.MaskNamespace)) {
                    skip = input.ReadUint32(); // namespace
                    input.SkipRawBytes(skip);
                }
                if (DsonInternals.IsEnabled(wireTypeBits, ObjectRef.MaskType)) {
                    input.ReadUint32();
                }
                if (DsonInternals.IsEnabled(wireTypeBits, ObjectRef.MaskPolicy)) {
                    input.ReadUint32();
                }
                return;
            }
            case DsonType.Timestamp: {
                input.ReadUint64();
                input.ReadUint32();
                input.ReadSint32();
                input.ReadUint32();
                return;
            }
            case DsonType.Binary:
            case DsonType.Array:
            case DsonType.Object:
            case DsonType.Header: {
                skip = input.ReadFixed32();
                break;
            }
            default: {
                throw DsonIOException.InvalidDsonType(contextType, dsonType);
            }
        }
        if (skip > 0) {
            input.SkipRawBytes(skip);
        }
    }

    public static DsonReaderGuide WhatShouldIDo(DsonContextType contextType, DsonReaderState state) {
        if (contextType == DsonContextType.TopLevel) {
            if (state == DsonReaderState.EndOfFile) {
                return DsonReaderGuide.Close;
            }
            if (state == DsonReaderState.Value) {
                return DsonReaderGuide.ReadValue;
            }
            return DsonReaderGuide.ReadType;
        }
        switch (state) {
            case DsonReaderState.Type: return DsonReaderGuide.ReadType;
            case DsonReaderState.Value: return DsonReaderGuide.ReadValue;
            case DsonReaderState.Name: return DsonReaderGuide.ReadName;
            case DsonReaderState.WaitStartObject: {
                if (contextType == DsonContextType.Header) {
                    return DsonReaderGuide.StartHeader;
                }
                if (contextType == DsonContextType.Array) {
                    return DsonReaderGuide.StartArray;
                }
                return DsonReaderGuide.StartObject;
            }
            case DsonReaderState.WaitEndObject: {
                if (contextType == DsonContextType.Header) {
                    return DsonReaderGuide.EndHeader;
                }
                if (contextType == DsonContextType.Array) {
                    return DsonReaderGuide.EndArray;
                }
                return DsonReaderGuide.EndObject;
            }
            case DsonReaderState.Initial:
            case DsonReaderState.EndOfFile:
            default:
                throw new InvalidOperationException("invalid state " + state);
        }
    }
}