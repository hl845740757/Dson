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

package cn.wjybxx.dson.codec.dson;

import cn.wjybxx.base.ObjectUtils;
import cn.wjybxx.dson.*;
import cn.wjybxx.dson.codec.*;
import cn.wjybxx.dson.text.DsonTextReader;
import cn.wjybxx.dson.text.DsonTexts;
import cn.wjybxx.dson.text.DsonToken;
import cn.wjybxx.dson.types.*;

import javax.annotation.Nonnull;
import javax.annotation.Nullable;
import java.util.List;
import java.util.Map;
import java.util.Objects;

/**
 * @author wjybxx
 * date - 2023/4/23
 */
abstract class AbstractObjectReader implements DsonObjectReader {

    protected DefaultDsonConverter converter;
    protected DsonReader reader;

    AbstractObjectReader(DefaultDsonConverter converter, DsonReader reader) {
        this.converter = converter;
        this.reader = reader;
    }

    // region 特殊接口

    @Override
    public void close() {
        reader.close();
    }

    @Override
    public ConverterOptions options() {
        return converter.options;
    }

    @Override
    public DsonType readDsonType() {
        return reader.isAtType() ? reader.readDsonType() : reader.getCurrentDsonType();
    }

    @Override
    public String readName() {
        return reader.isAtName() ? reader.readName() : reader.getCurrentName();
    }

    @Override
    @Nonnull
    public DsonType getCurrentDsonType() {
        return reader.getCurrentDsonType();
    }

    @Override
    public String getCurrentName() {
        return reader.getCurrentName();
    }

    @Override
    public DsonContextType getContextType() {
        return reader.getContextType();
    }

    @Override
    public void skipName() {
        reader.skipName();
    }

    @Override
    public void skipValue() {
        reader.skipValue();
    }

    @Override
    public void skipToEndOfObject() {
        reader.skipToEndOfObject();
    }

    @Override
    public byte[] readValueAsBytes(String name) {
        return readName(name) ? reader.readValueAsBytes(name) : null;
    }

    @SuppressWarnings("unchecked")
    @Override
    public <T> T decodeKey(String keyString, Class<T> keyDeclared) {
        if (keyDeclared == String.class || keyDeclared == Object.class) {
            return (T) keyString;
        }
        if (keyDeclared == Integer.class) { // key一定是包装类型
            return (T) Integer.valueOf(keyString);
        }
        if (keyDeclared == Long.class) {
            return (T) Long.valueOf(keyString);
        }
        DsonCodecImpl<T> codec = converter.codecRegistry.get(keyDeclared);
        if (codec == null || !codec.isEnumLiteCodec()) {
            throw DsonCodecException.unsupportedKeyType(keyDeclared);
        }
        int number = Integer.parseInt(keyString);
        T enumLite = codec.forNumber(number);
        if (enumLite == null) {
            throw DsonCodecException.enumAbsent(keyDeclared, number);
        }
        return enumLite;
    }

    @Override
    public void setComponentType(DsonType dsonType) {
        if (reader instanceof DsonTextReader textReader) {
            DsonToken token = DsonTexts.clsNameTokenOfType(dsonType);
            textReader.setCompClsNameToken(token);
        }
    }
    // endregion

    // region 简单值

    @Override
    public int readInt(String name) {
        return readName(name) ? DsonCodecHelper.readInt(reader, name) : 0;
    }

    @Override
    public long readLong(String name) {
        return readName(name) ? DsonCodecHelper.readLong(reader, name) : 0;
    }

    @Override
    public float readFloat(String name) {
        return readName(name) ? DsonCodecHelper.readFloat(reader, name) : 0;
    }

    @Override
    public double readDouble(String name) {
        return readName(name) ? DsonCodecHelper.readDouble(reader, name) : 0;
    }

    @Override
    public boolean readBoolean(String name) {
        return readName(name) && DsonCodecHelper.readBool(reader, name);
    }

    @Override
    public String readString(String name) {
        return readName(name) ? DsonCodecHelper.readString(reader, name) : null;
    }

    @Override
    public void readNull(String name) {
        if (readName(name)) {
            DsonCodecHelper.readNull(reader, name);
        }
    }

    @Override
    public Binary readBinary(String name) {
        return readName(name) ? DsonCodecHelper.readBinary(reader, name) : null;
    }

    @Override
    public ExtInt32 readExtInt32(String name) {
        return readName(name) ? DsonCodecHelper.readExtInt32(reader, name) : null;
    }

    @Override
    public ExtInt64 readExtInt64(String name) {
        return readName(name) ? DsonCodecHelper.readExtInt64(reader, name) : null;
    }

    @Override
    public ExtDouble readExtDouble(String name) {
        return readName(name) ? DsonCodecHelper.readExtDouble(reader, name) : null;
    }

    @Override
    public ExtString readExtString(String name) {
        return readName(name) ? DsonCodecHelper.readExtString(reader, name) : null;
    }

    @Override
    public ObjectRef readRef(String name) {
        return readName(name) ? DsonCodecHelper.readRef(reader, name) : null;
    }

    @Override
    public OffsetTimestamp readTimestamp(String name) {
        return readName(name) ? DsonCodecHelper.readTimestamp(reader, name) : null;
    }

    // endregion

    // region object处理

    @Override
    public <T> T readObject(TypeArgInfo<T> typeArgInfo) {
        Objects.requireNonNull(typeArgInfo);
        DsonType dsonType = reader.readDsonType();
        return readContainer(typeArgInfo, dsonType);
    }

    @SuppressWarnings("unchecked")
    @Nullable
    @Override
    public <T> T readObject(String name, TypeArgInfo<T> typeArgInfo) {
        Class<T> declaredType = typeArgInfo.declaredType;
        if (!readName(name)) {
            return (T) ConverterUtils.getDefaultValue(declaredType);
        }

        DsonReader reader = this.reader;
        // 基础类型不能返回null
        if (declaredType.isPrimitive()) {
            return (T) DsonCodecHelper.readPrimitive(reader, name, declaredType);
        }
        if (declaredType == String.class) {
            return (T) DsonCodecHelper.readString(reader, name);
        }
        if (declaredType == byte[].class) { // binary可以接收定长数据
            return (T) DsonCodecHelper.readBinary(reader, name);
        }

        // 对象类型--需要先读取写入的类型，才可以解码；Object上下文的话，readName已处理
        if (reader.getContextType() == DsonContextType.ARRAY) {
            if (reader.isAtType()) reader.readDsonType();
        }
        DsonType dsonType = reader.getCurrentDsonType();
        if (dsonType == DsonType.NULL) {
            return null;
        }
        if (dsonType.isContainer()) {
            // 容器类型只能通过codec解码
            return readContainer(typeArgInfo, dsonType);
        }
        // 考虑包装类型
        Class<?> unboxed = ConverterUtils.unboxIfWrapperType(declaredType);
        if (unboxed.isPrimitive()) {
            return (T) DsonCodecHelper.readPrimitive(reader, name, unboxed);
        }
        // 默认类型转换-声明类型可能是个抽象类型，eg：Number
        if (DsonValue.class.isAssignableFrom(declaredType)) {
            return declaredType.cast(Dsons.readDsonValue(reader));
        }
        return declaredType.cast(DsonCodecHelper.readValue(reader, dsonType, name));
    }

    private <T> T readContainer(TypeArgInfo<T> typeArgInfo, DsonType dsonType) {
        String classId = readClassId(dsonType);
        DsonCodecImpl<? extends T> codec = findObjectDecoder(typeArgInfo, dsonType, classId);
        if (codec == null) {
            throw DsonCodecException.incompatible(typeArgInfo.declaredType, classId);
        }
        return codec.readObject(this, typeArgInfo);
    }

    @Override
    public void readStartObject(@Nonnull TypeArgInfo<?> typeArgInfo) {
        if (reader.isAtType()) { // 顶层对象适配
            reader.readDsonType();
        }
        reader.readStartObject();
    }

    @Override
    public void readEndObject() {
        reader.skipToEndOfObject();
        reader.readEndObject();
    }

    @Override
    public void readStartArray(@Nonnull TypeArgInfo<?> typeArgInfo) {
        if (reader.isAtType()) { // 顶层对象适配
            reader.readDsonType();
        }
        reader.readStartArray();
    }

    @Override
    public void readEndArray() {
        reader.skipToEndOfObject();
        reader.readEndArray();
    }

    private String readClassId(DsonType dsonType) {
        DsonReader reader = this.reader;
        if (dsonType == DsonType.OBJECT) {
            reader.readStartObject();
        } else {
            reader.readStartArray();
        }
        String clsName;
        DsonType nextDsonType = reader.peekDsonType();
        if (nextDsonType == DsonType.HEADER) {
            reader.readDsonType();
            reader.readStartHeader();
            clsName = reader.readString(DsonHeader.NAMES_CLASS_NAME);
            reader.skipToEndOfObject();
            reader.readEndHeader();
        } else {
            clsName = "";
        }
        reader.backToWaitStart();
        return clsName;
    }

    @SuppressWarnings("unchecked")
    private <T> DsonCodecImpl<? extends T> findObjectDecoder(TypeArgInfo<T> typeArgInfo, DsonType dsonType, String classId) {
        final Class<T> declaredType = typeArgInfo.declaredType;
        if (!ObjectUtils.isBlank(classId)) {
            TypeMeta typeMeta = converter.typeMetaRegistry.ofName(classId);
            if (typeMeta != null && declaredType.isAssignableFrom(typeMeta.clazz)) {
                // 尝试按真实类型读
                return (DsonCodecImpl<? extends T>) converter.codecRegistry.get(typeMeta.clazz);
            }
        }
        if (declaredType == Object.class) {
            if (dsonType == DsonType.ARRAY) {
                return (DsonCodecImpl<? extends T>) converter.codecRegistry.get(List.class);
            } else {
                return (DsonCodecImpl<? extends T>) converter.codecRegistry.get(Map.class);
            }
        }
        // 尝试按照声明类型读 - 读的时候两者可能是无继承关系的(投影)
        return converter.codecRegistry.get(declaredType);
    }

    // endregion
}