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

package cn.wjybxx.dson;

import cn.wjybxx.dson.internal.InternalUtils;
import cn.wjybxx.dson.io.Chunk;
import cn.wjybxx.dson.io.DsonIOException;
import cn.wjybxx.dson.io.DsonInput;
import cn.wjybxx.dson.io.DsonOutput;
import cn.wjybxx.dson.types.ObjectRef;
import cn.wjybxx.dson.types.OffsetTimestamp;
import com.google.protobuf.CodedOutputStream;
import com.google.protobuf.MessageLite;
import com.google.protobuf.Parser;

import java.util.List;

/**
 * @author wjybxx
 * date - 2023/5/31
 */
public class DsonReaderUtils {

    /** 支持读取为bytes和直接写入bytes的数据类型 */
    private static final List<DsonType> VALUE_BYTES_TYPES = List.of(DsonType.STRING,
            DsonType.BINARY, DsonType.ARRAY, DsonType.OBJECT);

    // region 字节流

    public static int wireTypeOfFloat(float value) {
        int iv = (int) value;
        if (iv == value &&
                (iv >= Dsons.FLOAT_INLINE_MIN && iv <= Dsons.FLOAT_INLINE_MAX)) {
            return iv + Dsons.FLOAT_INLINE_OFFSET;
        }
        return 0;
    }

    public static int wireTypeOfDouble(double value) {
        int iv = (int) value;
        if (iv == value &&
                (iv >= Dsons.FLOAT_INLINE_MIN && iv <= Dsons.FLOAT_INLINE_MAX)) {
            return iv + Dsons.FLOAT_INLINE_OFFSET;
        }
        return 0;
    }

    public static float readFloat(DsonInput input, int wireTypeBits) {
        if (wireTypeBits == 0) {
            return input.readFloat();
        }
        return wireTypeBits - Dsons.FLOAT_INLINE_OFFSET;
    }

    public static double readDouble(DsonInput input, int wireTypeBits) {
        if (wireTypeBits == 0) {
            return input.readDouble();
        }
        return wireTypeBits - Dsons.FLOAT_INLINE_OFFSET;
    }

    public static boolean readBool(DsonInput input, int wireTypeBits) {
        if (wireTypeBits == 1) {
            return true;
        }
        if (wireTypeBits == 0) {
            return false;
        }
        throw new DsonIOException("invalid wireType of bool");
    }

    public static int wireTypeOfBinary(int type) {
        if (type <= Dsons.BINARY_INLINE_MAX) {
            return type + 1;
        }
        return 0;
    }

    public static int computeSizeOfBinaryType(int type) {
        if (type <= Dsons.BINARY_INLINE_MAX) {
            return 0;
        }
        return CodedOutputStream.computeUInt32SizeNoTag(type);
    }

    public static void writeBinary(DsonOutput output, DsonBinary binary) {
        int sizeOfBinaryType = computeSizeOfBinaryType(binary.getType());
        output.writeFixed32(sizeOfBinaryType + binary.getData().length);
        if (sizeOfBinaryType > 0) {
            output.writeUint32(binary.getType());
        }
        output.writeRawBytes(binary.getData());
    }

    public static void writeBinary(DsonOutput output, int type, Chunk chunk) {
        int sizeOfBinaryType = computeSizeOfBinaryType(type);
        output.writeFixed32(sizeOfBinaryType + chunk.getLength());
        if (sizeOfBinaryType > 0) {
            output.writeUint32(type);
        }
        output.writeRawBytes(chunk.getBuffer(), chunk.getOffset(), chunk.getLength());
    }

    public static DsonBinary readDsonBinary(DsonInput input, int currentWireTypeBits) {
        int size = input.readFixed32();
        int type = currentWireTypeBits > 0 ? currentWireTypeBits - 1 : input.readUint32();
        int dataLength = size - computeSizeOfBinaryType(type);
        return new DsonBinary(type,
                input.readRawBytes(dataLength));
    }

    public static void writeExtInt32(DsonOutput output, DsonExtInt32 extInt32, WireType wireType) {
        output.writeUint32(extInt32.getType());
        wireType.writeInt32(output, extInt32.getValue());
    }

    public static DsonExtInt32 readDsonExtInt32(DsonInput input, WireType wireType) {
        return new DsonExtInt32(
                input.readUint32(),
                wireType.readInt32(input));
    }

    public static void writeExtInt64(DsonOutput output, DsonExtInt64 extInt64, WireType wireType) {
        output.writeUint32(extInt64.getType());
        wireType.writeInt64(output, extInt64.getValue());
    }

    public static DsonExtInt64 readDsonExtInt64(DsonInput input, WireType wireType) {
        return new DsonExtInt64(
                input.readUint32(),
                wireType.readInt64(input));
    }

    public static int wireTypeOfExtString(DsonExtString extString) {
        int v = 0;
        if (extString.getType() != 0) {
            v |= DsonExtString.MASK_TYPE;
        }
        if (extString.hasValue()) {
            v |= DsonExtString.MASK_VALUE;
        }
        return v;
    }

    public static void writeExtString(DsonOutput output, DsonExtString extString) {
        if (extString.getType() != 0) {
            output.writeUint32(extString.getType());
        }
        if (extString.hasValue()) {
            output.writeString(extString.getValue());
        }
    }

    public static DsonExtString readDsonExtString(DsonInput input, int wireTypeBits) {
        int type = InternalUtils.isEnabled(wireTypeBits, DsonExtString.MASK_TYPE) ? input.readUint32() : 0;
        String value = InternalUtils.isEnabled(wireTypeBits, DsonExtString.MASK_VALUE) ? input.readString() : null;
        return new DsonExtString(type, value);
    }

    public static int wireTypeOfRef(ObjectRef objectRef) {
        int v = 0;
        if (objectRef.hasNamespace()) {
            v |= ObjectRef.MASK_NAMESPACE;
        }
        if (objectRef.getType() != 0) {
            v |= ObjectRef.MASK_TYPE;
        }
        if (objectRef.getPolicy() != 0) {
            v |= ObjectRef.MASK_POLICY;
        }
        return v;
    }

    public static void writeRef(DsonOutput output, ObjectRef objectRef) {
        output.writeString(objectRef.getLocalId());
        if (objectRef.hasNamespace()) {
            output.writeString(objectRef.getNamespace());
        }
        if (objectRef.getType() != 0) {
            output.writeUint32(objectRef.getType());
        }
        if (objectRef.getPolicy() != 0) {
            output.writeUint32(objectRef.getPolicy());
        }
    }

    public static ObjectRef readRef(DsonInput input, int wireTypeBits) {
        String localId = input.readString();
        String namespace = InternalUtils.isEnabled(wireTypeBits, ObjectRef.MASK_NAMESPACE) ? input.readString() : null;
        int type = InternalUtils.isEnabled(wireTypeBits, ObjectRef.MASK_TYPE) ? input.readUint32() : 0;
        int policy = InternalUtils.isEnabled(wireTypeBits, ObjectRef.MASK_POLICY) ? input.readUint32() : 0;
        return new ObjectRef(localId, namespace, type, policy);
    }

    public static void writeTimestamp(DsonOutput output, OffsetTimestamp timestamp) {
        output.writeUint64(timestamp.getSeconds());
        output.writeUint32(timestamp.getNanos());
        output.writeSint32(timestamp.getOffset());
        output.writeUint32(timestamp.getEnables());
    }

    public static OffsetTimestamp readTimestamp(DsonInput input) {
        return new OffsetTimestamp(
                input.readUint64(),
                input.readUint32(),
                input.readSint32(),
                input.readUint32());
    }

    public static void writeMessage(DsonOutput output, int binaryType, MessageLite messageLite) {
        int preWritten = output.getPosition();
        output.writeFixed32(0);
        output.writeRawByte(binaryType);
        output.writeMessageNoSize(messageLite);
        output.setFixedInt32(preWritten, output.getPosition() - preWritten - 4);
    }

    public static <T> T readMessage(DsonInput input, int binaryType, Parser<T> parser) {
        int size = input.readFixed32();
        int oldLimit = input.pushLimit(size);
        int subType = input.readUint8();
        if (subType != binaryType) {
            throw DsonIOException.unexpectedSubType(binaryType, subType);
        }
        T value = input.readMessageNoSize(parser);
        input.popLimit(oldLimit);
        return value;
    }

    public static void writeValueBytes(DsonOutput output, DsonType type, byte[] data) {
        if (type == DsonType.STRING) {
            output.writeUint32(data.length);
        } else {
            output.writeFixed32(data.length);
        }
        output.writeRawBytes(data);
    }

    public static byte[] readValueAsBytes(DsonInput input, DsonType dsonType) {
        int size;
        if (dsonType == DsonType.STRING) {
            size = input.readUint32();
        } else {
            size = input.readFixed32();
        }
        return input.readRawBytes(size);
    }

    public static void checkReadValueAsBytes(DsonType dsonType) {
        if (!VALUE_BYTES_TYPES.contains(dsonType)) {
            throw DsonIOException.invalidDsonType(VALUE_BYTES_TYPES, dsonType);
        }
    }

    public static void checkWriteValueAsBytes(DsonType dsonType) {
        if (!VALUE_BYTES_TYPES.contains(dsonType)) {
            throw DsonIOException.invalidDsonType(VALUE_BYTES_TYPES, dsonType);
        }
    }

    public static void skipToEndOfObject(DsonInput input) {
        int size = input.getBytesUntilLimit();
        if (size > 0) {
            input.skipRawBytes(size);
        }
    }

    public static void skipValue(DsonInput input, DsonContextType contextType,
                                 DsonType dsonType, WireType wireType, int wireTypeBits) {
        int skip;
        switch (dsonType) {
            case FLOAT -> {
                if (wireTypeBits != 0) {
                    return;
                }
                skip = 4;
            }
            case DOUBLE -> {
                if (wireTypeBits != 0) {
                    return;
                }
                skip = 8;
            }
            case BOOLEAN, NULL -> {
                return;
            }
            case INT32 -> {
                wireType.readInt32(input);
                return;
            }
            case INT64 -> {
                wireType.readInt64(input);
                return;
            }
            case EXT_INT32 -> {
                input.readUint32(); // 子类型
                wireType.readInt32(input);
                return;
            }
            case EXT_INT64 -> {
                input.readUint32(); // 子类型
                wireType.readInt64(input);
                return;
            }
            case STRING -> {
                skip = input.readUint32();  // string长度
            }
            case EXT_STRING -> {
                if (InternalUtils.isEnabled(wireTypeBits, DsonExtString.MASK_TYPE)) {
                    input.readUint32(); // 子类型
                }
                if (InternalUtils.isEnabled(wireTypeBits, DsonExtString.MASK_VALUE)) {
                    skip = input.readUint32(); // string长度
                } else {
                    skip = 0;
                }
            }
            case REFERENCE -> {
                skip = input.readUint32(); // localId
                input.skipRawBytes(skip);

                if (InternalUtils.isEnabled(wireTypeBits, ObjectRef.MASK_NAMESPACE)) {
                    skip = input.readUint32(); // namespace
                    input.skipRawBytes(skip);
                }
                if (InternalUtils.isEnabled(wireTypeBits, ObjectRef.MASK_TYPE)) {
                    input.readUint32();
                }
                if (InternalUtils.isEnabled(wireTypeBits, ObjectRef.MASK_POLICY)) {
                    input.readUint32();
                }
                return;
            }
            case TIMESTAMP -> {
                input.readUint64();
                input.readUint32();
                input.readSint32();
                input.readUint32();
                return;
            }

            case BINARY, ARRAY, OBJECT, HEADER -> {
                skip = input.readFixed32();
            }
            default -> {
                throw DsonIOException.invalidDsonType(contextType, dsonType);
            }
        }
        if (skip > 0) {
            input.skipRawBytes(skip);
        }
    }

    public static DsonReaderGuide whatShouldIDo(DsonContextType contextType, DsonReaderState state) {
        if (contextType == DsonContextType.TOP_LEVEL) {
            if (state == DsonReaderState.END_OF_FILE) {
                return DsonReaderGuide.CLOSE;
            }
            if (state == DsonReaderState.VALUE) {
                return DsonReaderGuide.READ_VALUE;
            }
            return DsonReaderGuide.READ_TYPE;
        } else {
            return switch (state) {
                case TYPE -> DsonReaderGuide.READ_TYPE;
                case VALUE -> DsonReaderGuide.READ_VALUE;
                case NAME -> DsonReaderGuide.READ_NAME;
                case WAIT_START_OBJECT -> {
                    if (contextType == DsonContextType.HEADER) {
                        yield DsonReaderGuide.START_HEADER;
                    }
                    if (contextType == DsonContextType.ARRAY) {
                        yield DsonReaderGuide.START_ARRAY;
                    }
                    yield DsonReaderGuide.START_OBJECT;
                }
                case WAIT_END_OBJECT -> {
                    if (contextType == DsonContextType.HEADER) {
                        yield DsonReaderGuide.END_HEADER;
                    }
                    if (contextType == DsonContextType.ARRAY) {
                        yield DsonReaderGuide.END_ARRAY;
                    }
                    yield DsonReaderGuide.END_OBJECT;
                }
                case INITIAL, END_OF_FILE -> throw new AssertionError("invalid state " + state);
            };
        }
    }

    // endregion
}