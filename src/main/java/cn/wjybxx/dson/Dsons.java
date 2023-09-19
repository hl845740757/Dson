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
import cn.wjybxx.dson.text.*;

import javax.annotation.Nullable;
import java.io.Reader;
import java.io.StringWriter;
import java.util.Objects;
import java.util.Properties;

/**
 * dson的辅助工具类，二进制流工具类{@link DsonLites}
 *
 * @author wjybxx
 * date - 2023/4/19
 */
@SuppressWarnings("unused")
public final class Dsons {

    /** {@link DsonType}占用的比特位 */
    public static final int DSON_TYPE_BITES = 5;
    /** {@link DsonType}的最大类型编号 */
    public static final int DSON_TYPE_MAX_VALUE = 31;

    /** {@link WireType}占位的比特位数 */
    public static final int WIRETYPE_BITS = 3;
    public static final int WIRETYPE_MASK = (1 << WIRETYPE_BITS) - 1;
    /** wireType看做数值时的最大值 */
    public static final int WIRETYPE_MAX_VALUE = 7;

    /** 完整类型信息占用的比特位数 */
    public static final int FULL_TYPE_BITS = DSON_TYPE_BITES + WIRETYPE_BITS;
    public static final int FULL_TYPE_MASK = (1 << FULL_TYPE_BITS) - 1;

    /**
     * 在文档数据解码中是否启用 字段字符串池化
     * 启用池化在大量相同字段名时可能很好地降低内存占用，默认开启
     * 字段名几乎都是常量，因此命中率几乎百分之百。
     */
    public static final boolean enableFieldIntern;
    /**
     * 在文档数据解码中是否启用 类型别名字符串池化
     * 类型数通常不多，开启池化对编解码性能影响较小，默认开启
     * 类型别名也基本是常量，因此命中率几乎百分之百
     */
    public static final boolean enableClassIntern;

    static {
        Properties properties = System.getProperties();
        enableFieldIntern = InternalUtils.getBool(properties, "cn.wjybxx.dson.enableFieldIntern", true);
        enableClassIntern = InternalUtils.getBool(properties, "cn.wjybxx.dson.enableClassIntern", true);
    }

    public static String internField(String fieldName) {
        return (enableFieldIntern && fieldName.length() <= 32) ? fieldName.intern() : fieldName;
    }

    public static String internClass(String className) {
        // 长度异常的数据不池化
        return (enableClassIntern && className.length() <= 128) ? className.intern() : className;
    }

    public static int checkSubType(int type) {
        if (type < 0) {
            throw new IllegalArgumentException("type cant be negative");
        }
        return type;
    }

    // fullType

    /**
     * @param dsonType 数据类型
     * @param wireType 特殊编码类型
     * @return fullType 完整类型
     */
    public static int makeFullType(DsonType dsonType, WireType wireType) {
        return (dsonType.getNumber() << WIRETYPE_BITS) | wireType.getNumber();
    }

    /**
     * @param dsonType 数据类型 5bits[0~31]
     * @param wireType 特殊编码类型 3bits[0~7]
     * @return fullType 完整类型
     */
    public static int makeFullType(int dsonType, int wireType) {
        return (dsonType << WIRETYPE_BITS) | wireType;
    }

    public static int dsonTypeOfFullType(int fullType) {
        return fullType >>> WIRETYPE_BITS;
    }

    public static int wireTypeOfFullType(int fullType) {
        return (fullType & WIRETYPE_MASK);
    }

    // region read/write

    public static void writeTopDsonValue(DsonWriter writer, DsonValue dsonValue, ObjectStyle style) {
        if (dsonValue.getDsonType() == DsonType.OBJECT) {
            writeObject(writer, dsonValue.asObject(), style);
        } else if (dsonValue.getDsonType() == DsonType.ARRAY) {
            writeArray(writer, dsonValue.asArray(), style);
        } else {
            writeHeader(writer, dsonValue.asHeader());
        }
    }

    /** @return 如果到达文件尾部，则返回null */
    public static DsonValue readTopDsonValue(DsonReader reader) {
        DsonType dsonType = reader.readDsonType();
        if (dsonType == DsonType.END_OF_OBJECT) {
            return null;
        }
        if (dsonType == DsonType.OBJECT) {
            return readObject(reader);
        } else if (dsonType == DsonType.ARRAY) {
            return readArray(reader);
        } else {
            assert dsonType == DsonType.HEADER;
            return readHeader(reader, new DsonHeader<>());
        }
    }

    /** 如果需要写入名字，外部写入 */
    public static void writeObject(DsonWriter writer, DsonObject<String> dsonObject, ObjectStyle style) {
        writer.writeStartObject(style);
        if (!dsonObject.getHeader().isEmpty()) {
            writeHeader(writer, dsonObject.getHeader());
        }
        dsonObject.getValueMap().forEach((name, dsonValue) -> writeDsonValue(writer, dsonValue, name));
        writer.writeEndObject();
    }

    public static DsonObject<String> readObject(DsonReader reader) {
        DsonObject<String> dsonObject = new DsonObject<>();
        DsonType dsonType;
        String name;
        DsonValue value;
        reader.readStartObject();
        while ((dsonType = reader.readDsonType()) != DsonType.END_OF_OBJECT) {
            if (dsonType == DsonType.HEADER) {
                readHeader(reader, dsonObject.getHeader());
            } else {
                name = reader.readName();
                value = readDsonValue(reader);
                dsonObject.put(name, value);
            }
        }
        reader.readEndObject();
        return dsonObject;
    }

    /** 如果需要写入名字，外部写入 */
    public static void writeArray(DsonWriter writer, DsonArray<String> dsonArray, ObjectStyle style) {
        writer.writeStartArray(style);
        if (!dsonArray.getHeader().isEmpty()) {
            writeHeader(writer, dsonArray.getHeader());
        }
        for (DsonValue dsonValue : dsonArray) {
            writeDsonValue(writer, dsonValue, null);
        }
        writer.writeEndArray();
    }

    public static DsonArray<String> readArray(DsonReader reader) {
        DsonArray<String> dsonArray = new DsonArray<>();
        DsonType dsonType;
        DsonValue value;
        reader.readStartArray();
        while ((dsonType = reader.readDsonType()) != DsonType.END_OF_OBJECT) {
            if (dsonType == DsonType.HEADER) {
                readHeader(reader, dsonArray.getHeader());
            } else {
                value = readDsonValue(reader);
                dsonArray.add(value);
            }
        }
        reader.readEndArray();
        return dsonArray;
    }

    public static void writeHeader(DsonWriter writer, DsonHeader<String> header) {
        if (header.size() == 1) {
            DsonValue clsName = header.get(DsonHeader.NAMES_CLASS_NAME);
            if (clsName != null) {
                writer.writeSimpleHeader(clsName.asString());
                return;
            }
        }
        writer.writeStartHeader(ObjectStyle.FLOW);
        header.getValueMap().forEach((key, value) -> writeDsonValue(writer, value, key));
        writer.writeEndHeader();
    }

    public static DsonHeader<String> readHeader(DsonReader reader, @Nullable DsonHeader<String> header) {
        if (header == null) header = new DsonHeader<>();
        DsonType dsonType;
        String name;
        DsonValue value;
        reader.readStartHeader();
        while ((dsonType = reader.readDsonType()) != DsonType.END_OF_OBJECT) {
            assert dsonType != DsonType.HEADER;
            name = reader.readName();
            value = readDsonValue(reader);
            header.put(name, value);
        }
        reader.readEndHeader();
        return header;
    }

    public static void writeDsonValue(DsonWriter writer, DsonValue dsonValue, @Nullable String name) {
        if (writer.isAtName()) {
            writer.writeName(name);
        }
        switch (dsonValue.getDsonType()) {
            case INT32 -> writer.writeInt32(name, dsonValue.asInt32(), WireType.VARINT, NumberStyle.TYPED);
            case INT64 -> writer.writeInt64(name, dsonValue.asInt64(), WireType.VARINT, NumberStyle.TYPED);
            case FLOAT -> writer.writeFloat(name, dsonValue.asFloat(), NumberStyle.TYPED);
            case DOUBLE -> writer.writeDouble(name, dsonValue.asDouble(), NumberStyle.SIMPLE);
            case BOOLEAN -> writer.writeBoolean(name, dsonValue.asBool());
            case STRING -> writer.writeString(name, dsonValue.asString(), StringStyle.AUTO);
            case NULL -> writer.writeNull(name);
            case BINARY -> writer.writeBinary(name, dsonValue.asBinary());
            case EXT_INT32 -> writer.writeExtInt32(name, dsonValue.asExtInt32(), WireType.VARINT, NumberStyle.SIMPLE);
            case EXT_INT64 -> writer.writeExtInt64(name, dsonValue.asExtInt64(), WireType.VARINT, NumberStyle.SIMPLE);
            case EXT_STRING -> writer.writeExtString(name, dsonValue.asExtString(), StringStyle.AUTO);
            case REFERENCE -> writer.writeRef(name, dsonValue.asReference());
            case TIMESTAMP -> writer.writeTimestamp(name, dsonValue.asTimestamp());
            case HEADER -> writeHeader(writer, dsonValue.asHeader());
            case ARRAY -> writeArray(writer, dsonValue.asArray(), ObjectStyle.INDENT);
            case OBJECT -> writeObject(writer, dsonValue.asObject(), ObjectStyle.INDENT);
            case END_OF_OBJECT -> throw new AssertionError();
        }
    }

    public static DsonValue readDsonValue(DsonReader reader) {
        DsonType dsonType = reader.getCurrentDsonType();
        reader.skipName();
        final String name = "";
        return switch (dsonType) {
            case INT32 -> new DsonInt32(reader.readInt32(name));
            case INT64 -> new DsonInt64(reader.readInt64(name));
            case FLOAT -> new DsonFloat(reader.readFloat(name));
            case DOUBLE -> new DsonDouble(reader.readDouble(name));
            case BOOLEAN -> new DsonBool(reader.readBoolean(name));
            case STRING -> new DsonString(reader.readString(name));
            case NULL -> {
                reader.readNull(name);
                yield DsonNull.INSTANCE;
            }
            case BINARY -> reader.readBinary(name);
            case EXT_INT32 -> reader.readExtInt32(name);
            case EXT_INT64 -> reader.readExtInt64(name);
            case EXT_STRING -> reader.readExtString(name);
            case REFERENCE -> new DsonReference(reader.readRef(name));
            case TIMESTAMP -> new DsonTimestamp(reader.readTimestamp(name));
            case HEADER -> {
                DsonHeader<String> header = new DsonHeader<>();
                readHeader(reader, header);
                yield header;
            }
            case OBJECT -> readObject(reader);
            case ARRAY -> readArray(reader);
            case END_OF_OBJECT -> throw new AssertionError();
        };
    }

    // endregion

    // region 拷贝

    public static DsonValue mutableDeepCopy(DsonValue dsonValue) {
        Objects.requireNonNull(dsonValue);
        switch (dsonValue.getDsonType()) {
            case OBJECT -> {
                return mutableDeepCopy(dsonValue.asObject());
            }
            case HEADER -> {
                return copyHeader(dsonValue.asHeader());
            }
            case ARRAY -> {
                return mutableDeepCopy(dsonValue.asArray());
            }
            case BINARY -> {
                return new DsonBinary(dsonValue.asBinary());
            }
            default -> {
                return dsonValue;
            }
        }
    }

    public static DsonObject<String> mutableDeepCopy(DsonObject<String> src) {
        DsonObject<String> result = new DsonObject<>(src.size());
        copyObject(src.getHeader(), result.getHeader());
        copyObject(src, result);
        return result;
    }

    public static DsonArray<String> mutableDeepCopy(DsonArray<String> src) {
        DsonArray<String> result = new DsonArray<>(src.size());
        copyObject(src.getHeader(), result.getHeader());
        if (src.size() > 0) {
            src.forEach(e -> result.add(mutableDeepCopy(e)));
        }
        return result;
    }

    private static DsonHeader<String> copyHeader(DsonHeader<String> src) {
        DsonHeader<String> result = new DsonHeader<>();
        copyObject(src, result);
        return result;
    }

    private static void copyObject(AbstractDsonObject<String> src, AbstractDsonObject<String> dest) {
        if (src.size() > 0) {
            src.forEach((s, dsonValue) -> dest.put(s, mutableDeepCopy(dsonValue)));
        }
    }

    // endregion

    // region 快捷方法

    public static String toDson(DsonValue dsonValue, ObjectStyle style) {
        return toDson(dsonValue, style, DsonTextWriterSettings.DEFAULT);
    }

    public static String toDson(DsonValue dsonValue, ObjectStyle style, DsonMode dsonMode) {
        return toDson(dsonValue, style, dsonMode == DsonMode.RELAXED
                ? DsonTextWriterSettings.RELAXED_DEFAULT
                : DsonTextWriterSettings.DEFAULT);
    }

    public static String toDson(DsonValue dsonValue, ObjectStyle style, DsonTextWriterSettings settings) {
        if (!dsonValue.getDsonType().isContainerOrHeader()) {
            throw new IllegalArgumentException("invalid dsonType " + dsonValue.getDsonType());
        }
        StringWriter stringWriter = new StringWriter(1024);
        try (DsonTextWriter writer = new DsonTextWriter(32, settings, stringWriter)) {
            writeTopDsonValue(writer, dsonValue, style);
        }
        return stringWriter.toString();
    }

    public static DsonValue fromDson(CharSequence dsonString) {
        try (DsonTextReader reader = new DsonTextReader(32, dsonString)) {
            return readTopDsonValue(reader);
        }
    }

    /** @param jsonString json字符串或无行首的dson字符串 */
    public static DsonValue fromJson(CharSequence jsonString) {
        try (DsonTextReader reader = new DsonTextReader(32, newJsonScanner(jsonString))) {
            return readTopDsonValue(reader);
        }
    }

    /** 获取dsonValue的localId -- dson的约定之一 */
    public static String getLocalId(DsonValue dsonValue) {
        DsonHeader<?> header;
        if (dsonValue instanceof DsonObject<?> dsonObject) {
            header = dsonObject.getHeader();
        } else if (dsonValue instanceof DsonArray<?> dsonArray) {
            header = dsonArray.getHeader();
        } else {
            return null;
        }
        DsonValue wrapped = header.get(DsonHeader.NAMES_LOCAL_ID);
        return wrapped instanceof DsonString dsonString ? dsonString.getValue() : null;
    }

    // endregion

    // region 工厂方法

    public static DsonScanner newStringScanner(CharSequence dsonString) {
        return new DsonScanner(DsonCharStream.newCharStream(dsonString, DsonMode.STANDARD));
    }

    public static DsonScanner newStringScanner(CharSequence dsonString, DsonMode dsonMode) {
        return new DsonScanner(DsonCharStream.newCharStream(dsonString, dsonMode));
    }

    public static DsonScanner newJsonScanner(CharSequence jsonString) {
        return new DsonScanner(DsonCharStream.newCharStream(jsonString, DsonMode.RELAXED));
    }

    public static DsonScanner newStreamScanner(Reader reader) {
        return new DsonScanner(DsonCharStream.newBufferedCharStream(reader, DsonMode.STANDARD));
    }

    public static DsonScanner newStreamScanner(Reader reader, DsonMode dsonMode) {
        return new DsonScanner(DsonCharStream.newBufferedCharStream(reader, dsonMode));
    }

    public static DsonScanner newStreamScanner(Reader reader, DsonMode dsonMode, int bufferSize, boolean autoClose) {
        return new DsonScanner(DsonCharStream.newBufferedCharStream(reader, dsonMode, bufferSize, autoClose));
    }

    // endregion

}