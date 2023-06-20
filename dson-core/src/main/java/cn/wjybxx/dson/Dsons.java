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
import java.io.StringWriter;
import java.util.Properties;

/**
 * <h3>Object编码</h3>
 * Lite版本：
 * <pre>
 *  length  [dsonType + wireType] +  [number  +  idep] + [length] + [subType] + [data] ...
 *  4Bytes    5 bits     3 bits       1~13 bits  3 bits   4 Bytes    1 Byte     0~n Bytes
 *  数据长度     1 Byte(unit8)           1 ~ 3 Byte          int32
 * </pre>
 * 1. 数组元素没有fullNumber
 * 2. fullNumber为uint32类型，1~3个字节
 * 3. 固定长度的属性类型没有length字段
 * 4. 数字没有length字段
 * 5. string是length是uint32变长编码。
 * 6. extInt32、extInt64和extString都是两个简单值的连续写入，subType采用uint32编码，value采用对应值类型的编码，extString的length不包含subType。
 * 7. binary的subType现固定1个Byte，以减小复杂度和节省开销。
 * 8. binary/Object/Array/header的length为fixed32编码，以方便扩展 - binary的length包含subType。
 * 9. header是object/array的一个匿名属性，在object中是没有字段id但有类型的值。
 * <p>
 * 普通版：
 * <pre>
 *  length  [dsonType + wireType] +  [length + name] +  [length] + [subType] + [data] ...
 *  4Bytes    5 bits     3 bits           nBytes         4 Bytes    1 Byte    0~n Bytes
 *  数据长度     1 Byte(unit8)             string          int32
 * </pre>
 * 其实就一个区别：普通版字段id采用String类型，而Lite版编码采用number类型。
 *
 * @author wjybxx
 * date - 2023/4/19
 */
public final class Dsons {

    /** {@link DsonType}的最大类型编号 */
    public static final int DSON_TYPE_MAX_VALUE = 31;
    /** {@link DsonType}占用的比特位 */
    private static final int DSON_TYPE_BITES = 5;

    /** {@link WireType}占位的比特位数 */
    static final int WIRETYPE_BITS = 3;
    static final int WIRETYPE_MASK = (1 << WIRETYPE_BITS) - 1;

    /** 完整类型信息占用的比特位数 */
    private static final int FULL_TYPE_BITS = DSON_TYPE_BITES + WIRETYPE_BITS;
    private static final int FULL_TYPE_MASK = (1 << FULL_TYPE_BITS) - 1;

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
        } else {
            writeArray(writer, dsonValue.asArray(), style);
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
        } else {
            assert dsonType == DsonType.ARRAY;
            return readArray(reader);
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
        DsonArray<String> dsonArray = new DsonArray<>(8);
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
            case INT32 -> writer.writeInt32(name, dsonValue.asInt32().getValue(), WireType.VARINT, NumberStyle.TYPED);
            case INT64 -> writer.writeInt64(name, dsonValue.asInt64().getValue(), WireType.VARINT, NumberStyle.TYPED);
            case FLOAT -> writer.writeFloat(name, dsonValue.asFloat().getValue(), NumberStyle.TYPED);
            case DOUBLE -> writer.writeDouble(name, dsonValue.asDouble().getValue());
            case BOOLEAN -> writer.writeBoolean(name, dsonValue.asBoolean().getValue());
            case STRING -> writer.writeString(name, dsonValue.asString().getValue(), StringStyle.AUTO);
            case NULL -> writer.writeNull(name);
            case BINARY -> writer.writeBinary(name, dsonValue.asBinary());
            case EXT_INT32 -> writer.writeExtInt32(name, dsonValue.asExtInt32(), WireType.VARINT);
            case EXT_INT64 -> writer.writeExtInt64(name, dsonValue.asExtInt64(), WireType.VARINT);
            case EXT_STRING -> writer.writeExtString(name, dsonValue.asExtString(), StringStyle.AUTO);
            case REFERENCE -> writer.writeRef(name, dsonValue.asReference().getValue());
            case TIMESTAMP -> writer.writeTimestamp(name, dsonValue.asTimestamp().getValue());
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
            case REFERENCE -> new DsonObjectRef(reader.readRef(name));
            case TIMESTAMP -> new DsonTimestamp(reader.readTimestamp(name));
            case HEADER -> {
                DsonHeader<String> header = new DsonHeader<String>();
                readHeader(reader, header);
                yield header;
            }
            case OBJECT -> readObject(reader);
            case ARRAY -> readArray(reader);
            case END_OF_OBJECT -> throw new AssertionError();
        };
    }

    public static String toDson(DsonValue dsonValue, ObjectStyle style) {
        return toDson(dsonValue, style, DsonTextWriterSettings.newBuilder().build());
    }

    public static String toDson(DsonValue dsonValue, ObjectStyle style, DsonTextWriterSettings settings) {
        if (!dsonValue.getDsonType().isContainer()) {
            throw new IllegalArgumentException("invalid dsonType " + dsonValue.getDsonType());
        }
        StringWriter stringWriter = new StringWriter(1024);
        try (DsonTextWriter writer = new DsonTextWriter(16, stringWriter, settings)) {
            writeTopDsonValue(writer, dsonValue, style);
        }
        return stringWriter.toString();
    }

    public static DsonValue fromDson(String dsonString) {
        try (DsonTextReader reader = new DsonTextReader(16, dsonString)) {
            return readTopDsonValue(reader);
        }
    }

    // endregion
}