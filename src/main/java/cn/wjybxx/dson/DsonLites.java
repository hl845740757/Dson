package cn.wjybxx.dson;

import cn.wjybxx.dson.text.NumberStyle;
import cn.wjybxx.dson.text.ObjectStyle;
import cn.wjybxx.dson.text.StringStyle;

import javax.annotation.Nullable;

/**
 * @author wjybxx
 * date - 2023/6/15
 */
public class DsonLites {

    /** 继承深度占用的比特位 */
    private static final int IDEP_BITS = 3;
    private static final int IDEP_MASK = (1 << IDEP_BITS) - 1;
    /**
     * 支持的最大继承深度 - 7
     * 1.idep的深度不包含Object，没有显式继承其它类的类，idep为0
     * 2.超过7层我认为是你的代码有问题，而不是框架问题
     */
    public static final int IDEP_MAX_VALUE = IDEP_MASK;

    /** 类字段最大number */
    public static final short LNUMBER_MAX_VALUE = 8191;
    /** 类字段占用的最大比特位数 - 暂不对外开放 */
    private static final int LNUMBER_MAX_BITS = 13;

    // fieldNumber

    /** 计算一个类的继承深度 */
    public static int calIdep(Class<?> clazz) {
        if (clazz.isInterface() || clazz.isPrimitive()) {
            throw new IllegalArgumentException();
        }
        if (clazz == Object.class) {
            return 0;
        }
        int r = -1; // 去除Object；简单说：Object和Object的直接子类的idep都记为0，这很有意义。
        while ((clazz = clazz.getSuperclass()) != null) {
            r++;
        }
        return r;
    }

    /**
     * @param idep    继承深度[0~7]
     * @param lnumber 字段在类本地的编号
     * @return fullNumber 字段的完整编号
     */
    public static int makeFullNumber(int idep, int lnumber) {
        return (lnumber << IDEP_BITS) | idep;
    }

    public static int lnumberOfFullNumber(int fullNumber) {
        return fullNumber >>> IDEP_BITS;
    }

    public static byte idepOfFullNumber(int fullNumber) {
        return (byte) (fullNumber & IDEP_MASK);
    }

    public static int makeFullNumberZeroIdep(int lnumber) {
        return lnumber << IDEP_BITS;
    }

    // classId
    public static long makeClassGuid(int namespace, int classId) {
        return ((long) namespace << 32) | ((long) classId & 0xFFFF_FFFFL);
    }

    public static int namespaceOfClassGuid(long guid) {
        return (int) (guid >>> 32);
    }

    public static int lclassIdOfClassGuid(long guid) {
        return (int) guid;
    }

    // region read/write

    public static void writeTopDsonValue(DsonLiteWriter writer, DsonValue dsonValue, ObjectStyle style) {
        if (dsonValue.getDsonType() == DsonType.OBJECT) {
            writeObject(writer, dsonValue.asObjectLite(), style);
        } else if (dsonValue.getDsonType() == DsonType.ARRAY) {
            writeArray(writer, dsonValue.asArrayLite(), style);
        } else {
            writeHeader(writer, dsonValue.asHeaderLite());
        }
    }

    /** @return 如果到达文件尾部，则返回null */
    public static DsonValue readTopDsonValue(DsonLiteReader reader) {
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
    public static void writeObject(DsonLiteWriter writer, DsonObject<FieldNumber> dsonObject, ObjectStyle style) {
        writer.writeStartObject(style);
        if (!dsonObject.getHeader().isEmpty()) {
            writeHeader(writer, dsonObject.getHeader());
        }
        dsonObject.getValueMap().forEach((name, dsonValue) -> writeDsonValue(writer, dsonValue, name.getFullNumber()));
        writer.writeEndObject();
    }

    public static DsonObject<FieldNumber> readObject(DsonLiteReader reader) {
        DsonObject<FieldNumber> dsonObject = new DsonObject<>();
        DsonType dsonType;
        FieldNumber name;
        DsonValue value;
        reader.readStartObject();
        while ((dsonType = reader.readDsonType()) != DsonType.END_OF_OBJECT) {
            if (dsonType == DsonType.HEADER) {
                readHeader(reader, dsonObject.getHeader());
            } else {
                name = FieldNumber.ofFullNumber(reader.readName());
                value = readDsonValue(reader);
                dsonObject.put(name, value);
            }
        }
        reader.readEndObject();
        return dsonObject;
    }

    /** 如果需要写入名字，外部写入 */
    public static void writeArray(DsonLiteWriter writer, DsonArray<FieldNumber> dsonArray, ObjectStyle style) {
        writer.writeStartArray(style);
        if (!dsonArray.getHeader().isEmpty()) {
            writeHeader(writer, dsonArray.getHeader());
        }
        for (DsonValue dsonValue : dsonArray) {
            writeDsonValue(writer, dsonValue, 0);
        }
        writer.writeEndArray();
    }

    public static DsonArray<FieldNumber> readArray(DsonLiteReader reader) {
        DsonArray<FieldNumber> dsonArray = new DsonArray<>(8);
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

    public static void writeHeader(DsonLiteWriter writer, DsonHeader<FieldNumber> header) {
        writer.writeStartHeader(ObjectStyle.FLOW);
        header.getValueMap().forEach((key, value) -> writeDsonValue(writer, value, key.getFullNumber()));
        writer.writeEndHeader();
    }

    public static DsonHeader<FieldNumber> readHeader(DsonLiteReader reader, @Nullable DsonHeader<FieldNumber> header) {
        if (header == null) header = new DsonHeader<>();
        DsonType dsonType;
        FieldNumber name;
        DsonValue value;
        reader.readStartHeader();
        while ((dsonType = reader.readDsonType()) != DsonType.END_OF_OBJECT) {
            assert dsonType != DsonType.HEADER;
            name = FieldNumber.ofFullNumber(reader.readName());
            value = readDsonValue(reader);
            header.put(name, value);
        }
        reader.readEndHeader();
        return header;
    }

    public static void writeDsonValue(DsonLiteWriter writer, DsonValue dsonValue, int name) {
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
            case HEADER -> writeHeader(writer, dsonValue.asHeaderLite());
            case ARRAY -> writeArray(writer, dsonValue.asArrayLite(), ObjectStyle.INDENT);
            case OBJECT -> writeObject(writer, dsonValue.asObjectLite(), ObjectStyle.INDENT);
            case END_OF_OBJECT -> throw new AssertionError();
        }
    }

    public static DsonValue readDsonValue(DsonLiteReader reader) {
        DsonType dsonType = reader.getCurrentDsonType();
        reader.skipName();
        final int name = 0;
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
                DsonHeader<FieldNumber> header = new DsonHeader<>();
                readHeader(reader, header);
                yield header;
            }
            case OBJECT -> readObject(reader);
            case ARRAY -> readArray(reader);
            case END_OF_OBJECT -> throw new AssertionError();
        };
    }
    // endregion
}