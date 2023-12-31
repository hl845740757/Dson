/*
 * Copyright 2023-2024 wjybxx(845740757@qq.com)
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

package cn.wjybxx.dson.codec.dsonlite;

import cn.wjybxx.dson.*;
import cn.wjybxx.dson.codec.*;
import cn.wjybxx.dson.io.DsonChunk;
import cn.wjybxx.dson.types.*;

import javax.annotation.Nonnull;
import javax.annotation.Nullable;
import java.util.Objects;

/**
 * @author wjybxx
 * date - 2023/4/23
 */
final class DefaultDsonLiteObjectWriter implements DsonLiteObjectWriter {

    private final DefaultDsonLiteConverter converter;
    private final DsonLiteWriter writer;

    public DefaultDsonLiteObjectWriter(DefaultDsonLiteConverter converter, DsonLiteWriter writer) {
        this.converter = converter;
        this.writer = writer;
    }

    // region 代理
    @Override
    public void flush() {
        writer.flush();
    }

    @Override
    public void close() {
        writer.close();
    }

    @Override
    public ConverterOptions options() {
        return converter.options;
    }

    @Override
    public int getCurrentName() {
        return writer.getCurrentName();
    }

    @Override
    public void writeName(int name) {
        writer.writeName(name);
    }

    @Override
    public void writeValueBytes(int name, DsonType dsonType, byte[] data) {
        Objects.requireNonNull(data);
        writer.writeValueBytes(name, dsonType, data);
    }
    // endregion

    // region 简单值

    @Override
    public void writeInt(int name, int value, WireType wireType) {
        if (value != 0 || (!writer.isAtName() || converter.options.appendDef)) {
            writer.writeInt32(name, value, wireType);
        }
    }

    @Override
    public void writeLong(int name, long value, WireType wireType) {
        if (value != 0 || (!writer.isAtName() || converter.options.appendDef)) {
            writer.writeInt64(name, value, wireType);
        }
    }

    @Override
    public void writeFloat(int name, float value) {
        if (value != 0 || (!writer.isAtName() || converter.options.appendDef)) {
            writer.writeFloat(name, value);
        }
    }

    @Override
    public void writeDouble(int name, double value) {
        if (value != 0 || (!writer.isAtName() || converter.options.appendDef)) {
            writer.writeDouble(name, value);
        }
    }

    @Override
    public void writeBoolean(int name, boolean value) {
        if (value || (!writer.isAtName() || converter.options.appendDef)) {
            writer.writeBoolean(name, value);
        }
    }

    @Override
    public void writeString(int name, @Nullable String value) {
        if (value == null) {
            writeNull(name);
        } else {
            writer.writeString(name, value);
        }
    }

    @Override
    public void writeNull(int name) {
        // 用户已写入name或convert开启了null写入
        if (!writer.isAtName() || converter.options.appendNull) {
            writer.writeNull(name);
        }
    }

    @Override
    public void writeBytes(int name, @Nullable byte[] value) {
        if (value == null) {
            writeNull(name);
        } else {
            writer.writeBinary(name, new Binary(0, value));
        }
    }

    @Override
    public void writeBytes(int name, int type, byte[] value) {
        if (value == null) {
            writeNull(name);
        } else {
            writer.writeBinary(name, new Binary(type, value));
        }
    }

    @Override
    public void writeBytes(int name, int type, @Nonnull DsonChunk chunk) {
        writer.writeBinary(name, type, chunk);
    }

    @Override
    public void writeBinary(int name, Binary binary) {
        if (binary == null) {
            writeNull(name);
        } else {
            writer.writeBinary(name, binary);
        }
    }

    @Override
    public void writeExtInt32(int name, ExtInt32 value, WireType wireType) {
        if (value == null) {
            writeNull(name);
        } else {
            writer.writeExtInt32(name, value, wireType);
        }
    }

    @Override
    public void writeExtInt32(int name, int type, int value, WireType wireType) {
        writer.writeExtInt32(name, new ExtInt32(type, value), wireType);
    }

    @Override
    public void writeExtInt64(int name, ExtInt64 value, WireType wireType) {
        if (value == null) {
            writeNull(name);
        } else {
            writer.writeExtInt64(name, value, wireType);
        }
    }

    @Override
    public void writeExtInt64(int name, int type, long value, WireType wireType) {
        writer.writeExtInt64(name, new ExtInt64(type, value), wireType);
    }

    @Override
    public void writeExtDouble(int name, ExtDouble value) {
        if (value == null) {
            writeNull(name);
        } else {
            writer.writeExtDouble(name, value);
        }
    }

    @Override
    public void writeExtDouble(int name, int type, double value) {
        writer.writeExtDouble(name, new ExtDouble(type, value));
    }

    @Override
    public void writeExtString(int name, ExtString value) {
        if (value == null) {
            writeNull(name);
        } else {
            writer.writeExtString(name, value);
        }
    }

    @Override
    public void writeExtString(int name, int type, String value) {
        writer.writeExtString(name, new ExtString(type, value));
    }

    @Override
    public void writeRef(int name, ObjectRef objectRef) {
        if (objectRef == null) {
            writeNull(name);
        } else {
            writer.writeRef(name, objectRef);
        }
    }

    @Override
    public void writeTimestamp(int name, OffsetTimestamp timestamp) {
        if (timestamp == null) {
            writeNull(name);
        } else {
            writer.writeTimestamp(name, timestamp);
        }
    }
    // endregion

    // region object处理

    @Override
    public <T> void writeObject(T value, TypeArgInfo<?> typeArgInfo) {
        Objects.requireNonNull(value, "value is null");
        DsonLiteCodecImpl<? super T> codec = findObjectEncoder(value);
        if (codec == null) {
            throw DsonCodecException.unsupportedType(value.getClass());
        }
        codec.writeObject(this, value, typeArgInfo);
    }

    @Override
    public <T> void writeObject(int name, T value, TypeArgInfo<?> typeArgInfo) {
        Objects.requireNonNull(typeArgInfo, "typeArgInfo");
        if (value == null) {
            writeNull(name);
            return;
        }
        // 由于基本类型通常会使用特定的read/write方法，因此最后测试基本类型和包装类型
        DsonLiteCodecImpl<? super T> codec = findObjectEncoder(value);
        if (codec != null) {
            writer.writeName(name);
            codec.writeObject(this, value, typeArgInfo);
            return;
        }

        Class<?> type = value.getClass();
        if (type == Integer.class) {
            writeInt(name, (Integer) value);
            return;
        }
        if (type == Long.class) {
            writeLong(name, (Long) value);
            return;
        }
        if (type == Float.class) {
            writeFloat(name, (Float) value);
            return;
        }
        if (type == Double.class) {
            writeDouble(name, (Double) value);
            return;
        }
        if (type == Boolean.class) {
            writeBoolean(name, (Boolean) value);
            return;
        }
        //
        if (type == String.class) {
            writeString(name, (String) value);
            return;
        }
        if (type == byte[].class) {
            writeBytes(name, (byte[]) value);
            return;
        }
        //
        if (type == Short.class) {
            writeShort(name, (Short) value);
            return;
        }
        if (type == Byte.class) {
            writeByte(name, (Byte) value);
            return;
        }
        if (type == Character.class) {
            writeChar(name, (Character) value);
            return;
        }
        if (value instanceof DsonValue dsonValue) {
            DsonLites.writeDsonValue(writer, dsonValue, name);
            return;
        }
        throw DsonCodecException.unsupportedType(type);
    }

    @Override
    public void writeStartObject(@Nonnull Object value, TypeArgInfo<?> typeArgInfo) {
        writer.writeStartObject();
        writeClassId(value, typeArgInfo);
    }

    @Override
    public void writeEndObject() {
        writer.writeEndObject();
    }

    @Override
    public void writeStartArray(@Nonnull Object value, TypeArgInfo<?> typeArgInfo) {
        writer.writeStartArray();
        writeClassId(value, typeArgInfo);
    }

    @Override
    public void writeEndArray() {
        writer.writeEndArray();
    }

    private void writeClassId(Object value, TypeArgInfo<?> typeArgInfo) {
        final Class<?> encodeClass = ConverterUtils.getEncodeClass(value); // 小心枚举
        if (!converter.options.classIdPolicy.test(typeArgInfo.declaredType, encodeClass)) {
            return;
        }
        TypeMeta typeMeta = converter.typeMetaRegistry.ofType(encodeClass);
        if (typeMeta != null && !typeMeta.classIds.isEmpty()) {
            long classGuid = converter.options.classIdConverter.toGuid(typeMeta.mainClassId());
            writer.writeStartHeader();
            writer.writeInt64(DsonHeader.NUMBERS_CLASS_ID, classGuid, WireType.UINT);
            writer.writeEndHeader();
        }
    }

    @SuppressWarnings("unchecked")
    private <T> DsonLiteCodecImpl<? super T> findObjectEncoder(T value) {
        final Class<?> encodeClass = ConverterUtils.getEncodeClass(value); // 小心枚举...
        return (DsonLiteCodecImpl<? super T>) converter.codecRegistry.get(encodeClass);
    }
    // endregion

}