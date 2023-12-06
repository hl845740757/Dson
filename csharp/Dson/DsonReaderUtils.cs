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

using System.Collections.Immutable;
using Dson.IO;
using Dson.Types;
using Google.Protobuf;

namespace Dson;

public class DsonReaderUtils
{
    /** 支持读取为bytes和直接写入bytes的数据类型 -- 这些类型不可以存储额外数据在WireType上 */
    public static readonly IList<DsonType> VALUE_BYTES_TYPES = new[] {
        DsonType.STRING, DsonType.BINARY, DsonType.ARRAY, DsonType.OBJECT, DsonType.HEADER
    }.ToImmutableList();

    // region number

    public static void writeInt32(IDsonOutput output, int value, WireType wireType) {
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

    public static int readInt32(IDsonInput input, WireType wireType) {
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

    public static void writeInt64(IDsonOutput output, long value, WireType wireType) {
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

    public static long readInt64(IDsonInput input, WireType wireType) {
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
    public static int wireTypeOfFloat(float value) {
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
    public static void writeFloat(IDsonOutput output, float value, int wireType) {
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

    public static float readFloat(IDsonInput input, int wireType) {
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
    public static int wireTypeOfDouble(double value) {
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

    public static void writeDouble(IDsonOutput output, double value, int wireType) {
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

    public static double readDouble(IDsonInput input, int wireType) {
        if (wireType == 0) {
            return input.ReadDouble();
        }

        long rawBits = 0;
        for (int i = wireType; i < 8; i++) {
            rawBits |= (input.ReadRawByte() & 0XFFL) << (8 * i);
        }
        return BitConverter.Int64BitsToDouble(rawBits);
    }

    public static bool readBool(IDsonInput input, int wireTypeBits) {
        if (wireTypeBits == 1) {
            return true;
        }
        if (wireTypeBits == 0) {
            return false;
        }
        throw new DsonIOException("invalid wireType for bool, bits: " + wireTypeBits);
    }
    // endregion

    // region binary

    public static void writeBinary(IDsonOutput output, DsonBinary binary) {
        int sizeOfBinaryType = CodedOutputStream.ComputeUInt32Size((uint)binary.Type);
        output.WriteFixed32(sizeOfBinaryType + binary.Data.Length);
        output.WriteUint32(binary.Type);
        output.WriteRawBytes(binary.Data);
    }

    public static void writeBinary(IDsonOutput output, int type, DsonChunk chunk) {
        int sizeOfBinaryType = CodedOutputStream.ComputeUInt32Size((uint)type);
        output.WriteFixed32(sizeOfBinaryType + chunk.Length);
        output.WriteUint32(type);
        output.WriteRawBytes(chunk.Buffer, chunk.Offset, chunk.Length);
    }

    public static DsonBinary readDsonBinary(IDsonInput input) {
        int size = input.ReadFixed32();
        int oldLimit = input.PushLimit(size);
        DsonBinary binary;
        {
            int type = input.ReadUint32();
            int dataLength = input.GetBytesUntilLimit();
            binary = new DsonBinary(type, input.ReadRawBytes(dataLength));
        }
        input.PopLimit(oldLimit);
        return binary;
    }

    public static void writeMessage(IDsonOutput output, int binaryType, IMessage messageLite) {
        int sizeOfBinaryType = CodedOutputStream.ComputeUInt32Size((uint)binaryType);
        output.WriteFixed32(sizeOfBinaryType + messageLite.CalculateSize()); // CalculateSize，因此这里调用不会增加开销
        output.WriteUint32(binaryType);
        output.WriteMessage(messageLite);
    }

    public static T readMessage<T>(IDsonInput input, int binaryType, MessageParser<T> parser) where T : IMessage<T> {
        int size = input.ReadFixed32();
        int oldLimit = input.PushLimit(size);
        T value;
        {
            int type = input.ReadUint32();
            if (type != binaryType) throw DsonIOException.unexpectedSubType(binaryType, type);
            value = input.ReadMessage(parser);
        }
        input.PopLimit(oldLimit);
        return value;
    }

    // endregion

    public static void writeExtInt32(IDsonOutput output, DsonExtInt32 extInt32, WireType wireType) {
        output.WriteUint32(extInt32.Type);
        output.WriteBool(extInt32.HasValue);
        if (extInt32.HasValue) {
            writeInt32(output, extInt32.Value, wireType);
        }
    }

    public static DsonExtInt32 readDsonExtInt32(IDsonInput input, WireType wireType) {
        int type = input.ReadUint32();
        bool hasValue = input.ReadBool();
        int value = hasValue ? readInt32(input, wireType) : 0;
        return new DsonExtInt32(type, value, hasValue);
    }

    public static void writeExtInt64(IDsonOutput output, DsonExtInt64 extInt64, WireType wireType) {
        output.WriteUint32(extInt64.Type);
        output.WriteBool(extInt64.HasValue);
        if (extInt64.HasValue) {
            writeInt64(output, extInt64.Value, wireType);
        }
    }

    public static DsonExtInt64 readDsonExtInt64(IDsonInput input, WireType wireType) {
        int type = input.ReadUint32();
        bool hasValue = input.ReadBool();
        long value = hasValue ? readInt64(input, wireType) : 0;
        return new DsonExtInt64(type, value, hasValue);
    }

    public static void writeExtDouble(IDsonOutput output, DsonExtDouble extDouble, int wireType) {
        output.WriteUint32(extDouble.Type);
        output.WriteBool(extDouble.HasValue);
        if (extDouble.HasValue) {
            writeDouble(output, extDouble.Value, wireType);
        }
    }

    public static DsonExtDouble readDsonExtDouble(IDsonInput input, int wireType) {
        int type = input.ReadUint32();
        bool hasValue = input.ReadBool();
        double value = hasValue ? readDouble(input, wireType) : 0;
        return new DsonExtDouble(type, value, hasValue);
    }

    public static int wireTypeOfExtString(DsonExtString extString) {
        int v = 0;
        if (extString.Type != 0) {
            v |= DsonExtString.MASK_TYPE;
        }
        if (extString.HasValue) {
            v |= DsonExtString.MASK_VALUE;
        }
        return v;
    }

    public static void writeExtString(IDsonOutput output, DsonExtString extString) {
        if (extString.Type != 0) {
            output.WriteUint32(extString.Type);
        }
        if (extString.HasValue) {
            output.WriteString(extString.Value!);
        }
    }

    public static DsonExtString readDsonExtString(IDsonInput input, int wireTypeBits) {
        int type = DsonInternals.isEnabled(wireTypeBits, DsonExtString.MASK_TYPE) ? input.ReadUint32() : 0;
        string value = DsonInternals.isEnabled(wireTypeBits, DsonExtString.MASK_VALUE) ? input.ReadString() : null;
        return new DsonExtString(type, value);
    }

    public static int wireTypeOfRef(ObjectRef objectRef) {
        int v = 0;
        if (objectRef.hasNamespace) {
            v |= ObjectRef.MASK_NAMESPACE;
        }
        if (objectRef.Type != 0) {
            v |= ObjectRef.MASK_TYPE;
        }
        if (objectRef.Policy != 0) {
            v |= ObjectRef.MASK_POLICY;
        }
        return v;
    }

    public static void writeRef(IDsonOutput output, ObjectRef objectRef) {
        output.WriteString(objectRef.LocalId);
        if (objectRef.hasNamespace) {
            output.WriteString(objectRef.Namespace);
        }
        if (objectRef.Type != 0) {
            output.WriteUint32(objectRef.Type);
        }
        if (objectRef.Policy != 0) {
            output.WriteUint32(objectRef.Policy);
        }
    }

    public static ObjectRef readRef(IDsonInput input, int wireTypeBits) {
        string localId = input.ReadString();
        string ns = DsonInternals.isEnabled(wireTypeBits, ObjectRef.MASK_NAMESPACE) ? input.ReadString() : null;
        int type = DsonInternals.isEnabled(wireTypeBits, ObjectRef.MASK_TYPE) ? input.ReadUint32() : 0;
        int policy = DsonInternals.isEnabled(wireTypeBits, ObjectRef.MASK_POLICY) ? input.ReadUint32() : 0;
        return new ObjectRef(localId, ns, type, policy);
    }

    public static void writeTimestamp(IDsonOutput output, OffsetTimestamp timestamp) {
        output.WriteUint64(timestamp.Seconds);
        output.WriteUint32(timestamp.Nanos);
        output.WriteSint32(timestamp.Offset);
        output.WriteUint32(timestamp.Enables);
    }

    public static OffsetTimestamp readTimestamp(IDsonInput input) {
        return new OffsetTimestamp(
            input.ReadUint64(),
            input.ReadUint32(),
            input.ReadSint32(),
            input.ReadUint32());
    }

    public static void writeValueBytes(IDsonOutput output, DsonType type, byte[] data) {
        if (type == DsonType.STRING) {
            output.WriteUint32(data.Length);
        }
        else {
            output.WriteFixed32(data.Length);
        }
        output.WriteRawBytes(data);
    }

    public static byte[] readValueAsBytes(IDsonInput input, DsonType dsonType) {
        int size;
        if (dsonType == DsonType.STRING) {
            size = input.ReadUint32();
        }
        else {
            size = input.ReadFixed32();
        }
        return input.ReadRawBytes(size);
    }

    public static void checkReadValueAsBytes(DsonType dsonType) {
        if (!VALUE_BYTES_TYPES.Contains(dsonType)) {
            throw DsonIOException.invalidDsonType(VALUE_BYTES_TYPES, dsonType);
        }
    }

    public static void checkWriteValueAsBytes(DsonType dsonType) {
        if (!VALUE_BYTES_TYPES.Contains(dsonType)) {
            throw DsonIOException.invalidDsonType(VALUE_BYTES_TYPES, dsonType);
        }
    }

    public static void skipToEndOfObject(IDsonInput input) {
        int size = input.GetBytesUntilLimit();
        if (size > 0) {
            input.SkipRawBytes(size);
        }
    }

    public static void skipValue(IDsonInput input, DsonContextType contextType,
                                 DsonType dsonType, WireType wireType, int wireTypeBits) {
        int skip;
        switch (dsonType) {
            case DsonType.FLOAT: {
                skip = 4 - wireTypeBits;
                break;
            }
            case DsonType.DOUBLE: {
                skip = 8 - wireTypeBits;
                break;
            }
            case DsonType.BOOLEAN:
            case DsonType.NULL: {
                return;
            }
            case DsonType.INT32: {
                readInt32(input, wireType);
                return;
            }
            case DsonType.INT64: {
                readInt64(input, wireType);
                return;
            }
            case DsonType.STRING: {
                skip = input.ReadUint32(); // string长度
                break;
            }
            case DsonType.EXT_INT32: {
                input.ReadUint32(); // 子类型
                if (input.ReadBool()) {
                    readInt32(input, wireType);
                }
                return;
            }
            case DsonType.EXT_INT64: {
                input.ReadUint32(); // 子类型
                if (input.ReadBool()) {
                    readInt64(input, wireType);
                }
                return;
            }
            case DsonType.EXT_DOUBLE: {
                input.ReadUint32(); // 子类型
                if (input.ReadBool()) {
                    skip = 8 - wireTypeBits;
                }
                else {
                    skip = 0;
                }
                break;
            }
            case DsonType.EXT_STRING: {
                if (DsonInternals.isEnabled(wireTypeBits, DsonExtString.MASK_TYPE)) {
                    input.ReadUint32(); // 子类型
                }
                if (DsonInternals.isEnabled(wireTypeBits, DsonExtString.MASK_VALUE)) {
                    skip = input.ReadUint32(); // string长度
                    input.SkipRawBytes(skip);
                }
                return;
            }
            case DsonType.REFERENCE: {
                skip = input.ReadUint32(); // localId
                input.SkipRawBytes(skip);

                if (DsonInternals.isEnabled(wireTypeBits, ObjectRef.MASK_NAMESPACE)) {
                    skip = input.ReadUint32(); // namespace
                    input.SkipRawBytes(skip);
                }
                if (DsonInternals.isEnabled(wireTypeBits, ObjectRef.MASK_TYPE)) {
                    input.ReadUint32();
                }
                if (DsonInternals.isEnabled(wireTypeBits, ObjectRef.MASK_POLICY)) {
                    input.ReadUint32();
                }
                return;
            }
            case DsonType.TIMESTAMP: {
                input.ReadUint64();
                input.ReadUint32();
                input.ReadSint32();
                input.ReadUint32();
                return;
            }
            case DsonType.BINARY:
            case DsonType.ARRAY:
            case DsonType.OBJECT:
            case DsonType.HEADER: {
                skip = input.ReadFixed32();
                break;
            }
            default: {
                throw DsonIOException.invalidDsonType(contextType, dsonType);
            }
        }
        if (skip > 0) {
            input.SkipRawBytes(skip);
        }
    }
}