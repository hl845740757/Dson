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

using System.Diagnostics;
using Dson.Text;

namespace Dson;

/// <summary>
/// C#是真泛型，因此可以减少类型
/// </summary>
public static class Dsons
{
    #region 基础常量

    /** {@link DsonType}占用的比特位 */
    public const int DSON_TYPE_BITES = 5;
    /** {@link DsonType}的最大类型编号 */
    public const int DSON_TYPE_MAX_VALUE = 31;

    /** {@link WireType}占位的比特位数 */
    public const int WIRETYPE_BITS = 3;
    public const int WIRETYPE_MASK = (1 << WIRETYPE_BITS) - 1;
    /** wireType看做数值时的最大值 */
    public const int WIRETYPE_MAX_VALUE = 7;

    /** 完整类型信息占用的比特位数 */
    public const int FULL_TYPE_BITS = DSON_TYPE_BITES + WIRETYPE_BITS;
    public const int FULL_TYPE_MASK = (1 << FULL_TYPE_BITS) - 1;

    /** 二进制数据的最大长度 -- type使用 varint编码，最大占用5个字节 */
    public const int MAX_BINARY_LENGTH = int.MaxValue - 5;

    #endregion

    #region 二进制常量

    /** 继承深度占用的比特位 */
    private const int IDEP_BITS = 3;
    private const int IDEP_MASK = (1 << IDEP_BITS) - 1;
    /**
     * 支持的最大继承深度 - 7
     * 1.idep的深度不包含Object，没有显式继承其它类的类，idep为0
     * 2.超过7层我认为是你的代码有问题，而不是框架问题
     */
    public const int IDEP_MAX_VALUE = IDEP_MASK;

    /** 类字段最大number */
    private const short LNUMBER_MAX_VALUE = 8191;
    /** 类字段占用的最大比特位数 - 暂不对外开放 */
    private const int LNUMBER_MAX_BITS = 13;

    #endregion

    #region Other

    /// <summary>
    /// 池化字段名；避免创建大量相同内容的字符串，有一定的查找开销，但对内存友好
    /// </summary>
    /// <param name="fieldName"></param>
    /// <returns></returns>
    public static string InternField(string fieldName) {
        // 长度异常的数据不池化
        return fieldName.Length <= 32 ? string.Intern(fieldName) : fieldName;
    }

    public static void CheckSubType(int type) {
        if (type < 0) {
            throw new ArgumentException("type cant be negative");
        }
    }

    public static void CheckBinaryLength(int length) {
        if (length > MAX_BINARY_LENGTH) {
            throw new ArgumentException($"the length of data must between[0, {MAX_BINARY_LENGTH}], but found: {length}");
        }
    }

    public static void CheckHasValue(int value, bool hasVal) {
        if (!hasVal && value != 0) {
            throw new ArgumentException();
        }
    }

    public static void CheckHasValue(long value, bool hasVal) {
        if (!hasVal && value != 0) {
            throw new ArgumentException();
        }
    }

    public static void CheckHasValue(double value, bool hasVal) {
        if (!hasVal && value != 0) {
            throw new ArgumentException();
        }
    }

    #endregion

    #region FullType

    /// <summary>
    /// 计算FullType     
    /// </summary>
    /// <param name="dsonType">Dson数据类型</param>
    /// <param name="wireType">特殊编码类型</param>
    /// <returns>完整类型</returns>
    public static int MakeFullType(DsonType dsonType, WireType wireType) {
        return ((int)dsonType << WIRETYPE_BITS) | (int)wireType;
    }

    /// <summary>
    /// 用于非常规类型计算FullType
    /// </summary>
    /// <param name="dsonType">Dson数据类型 5bits[0~31]</param>
    /// <param name="wireType">特殊编码类型 3bits[0~7]</param>
    /// <returns>完整类型</returns>
    public static int MakeFullType(int dsonType, int wireType) {
        return (dsonType << WIRETYPE_BITS) | wireType;
    }

    public static int DsonTypeOfFullType(int fullType) {
        return DsonInternals.LogicalShiftRight(fullType, WIRETYPE_BITS);
    }

    public static int WireTypeOfFullType(int fullType) {
        return (fullType & WIRETYPE_MASK);
    }

    #endregion

    #region FieldNumber

    /// <summary>
    /// 计算字段的完整数字
    /// </summary>
    /// <param name="idep">继承深度[0~7]</param>
    /// <param name="lnumber">字段在类本地的编号</param>
    /// <returns>字段的完整编号</returns>
    public static int MakeFullNumber(int idep, int lnumber) {
        return (lnumber << IDEP_BITS) | idep;
    }

    public static int LnumberOfFullNumber(int fullNumber) {
        return DsonInternals.LogicalShiftRight(fullNumber, IDEP_BITS);
    }

    public static byte IdepOfFullNumber(int fullNumber) {
        return (byte)(fullNumber & IDEP_MASK);
    }

    public static int MakeFullNumberZeroIdep(int lnumber) {
        return lnumber << IDEP_BITS;
    }

    // classId
    public static long MakeClassGuid(int ns, int classId) {
        return ((long)ns << 32) | ((long)classId & 0xFFFF_FFFFL);
    }

    public static int NamespaceOfClassGuid(long guid) {
        return (int)DsonInternals.LogicalShiftRight(guid, 32);
    }

    public static int LclassIdOfClassGuid(long guid) {
        return (int)guid;
    }

    /// <summary>
    /// 计算一个类的继承深度
    /// </summary>
    /// <param name="clazz"></param>
    /// <returns></returns>
    public static int CalIdep(Type clazz) {
        if (clazz.IsInterface || clazz.IsPrimitive) {
            throw new ArgumentException();
        }
        if (clazz == typeof(object)) {
            return 0;
        }
        int r = -1; // 去除Object；简单说：Object和Object的直接子类的idep都记为0，这很有意义。
        while ((clazz = clazz.BaseType) != null) {
            r++;
        }
        return r;
    }

    #endregion

    #region Read/Write

    public static void writeTopDsonValue<TName>(IDsonWriter<TName> writer,
                                                DsonValue dsonValue, ObjectStyle style) where TName : IEquatable<TName> {
        if (dsonValue.DsonType == DsonType.OBJECT) {
            writeObject(writer, dsonValue.AsObject<TName>(), style);
        }
        else if (dsonValue.DsonType == DsonType.ARRAY) {
            writeArray(writer, dsonValue.AsArray<TName>(), style);
        }
        else {
            writeHeader(writer, dsonValue.AsHeader<TName>());
        }
    }

    /** @return 如果到达文件尾部，则返回null */
    public static DsonValue? readTopDsonValue<TName>(IDsonReader<TName> reader) where TName : IEquatable<TName> {
        DsonType dsonType = reader.ReadDsonType();
        if (dsonType == DsonType.END_OF_OBJECT) {
            return null;
        }
        if (dsonType == DsonType.OBJECT) {
            return readObject(reader);
        }
        else if (dsonType == DsonType.ARRAY) {
            return readArray(reader);
        }
        else {
            Debug.Assert(dsonType == DsonType.HEADER);
            return readHeader(reader, new DsonHeader<TName>());
        }
    }

    /** 如果需要写入名字，外部写入 */
    public static void writeObject<TName>(IDsonWriter<TName> writer,
                                          DsonObject<TName> dsonObject, ObjectStyle style) where TName : IEquatable<TName> {
        writer.WriteStartObject(style);
        if (dsonObject.Header.Count > 0) {
            writeHeader(writer, dsonObject.Header);
        }
        foreach (var pair in dsonObject) {
            writeDsonValue(writer, pair.Value, pair.Key);
        }
        writer.WriteEndObject();
    }

    public static DsonObject<TName> readObject<TName>(IDsonReader<TName> reader) where TName : IEquatable<TName> {
        DsonObject<TName> dsonObject = new DsonObject<TName>();
        DsonType dsonType;
        TName name;
        DsonValue value;
        reader.ReadStartObject();
        while ((dsonType = reader.ReadDsonType()) != DsonType.END_OF_OBJECT) {
            if (dsonType == DsonType.HEADER) {
                readHeader(reader, dsonObject.Header);
            }
            else {
                name = reader.ReadName();
                value = readDsonValue(reader);
                dsonObject[name] = value;
            }
        }
        reader.ReadEndObject();
        return dsonObject;
    }

    /** 如果需要写入名字，外部写入 */
    public static void writeArray<TName>(IDsonWriter<TName> writer,
                                         DsonArray<TName> dsonArray, ObjectStyle style) where TName : IEquatable<TName> {
        writer.WriteStartArray(style);
        if (dsonArray.Header.Count > 0) {
            writeHeader(writer, dsonArray.Header);
        }
        foreach (DsonValue dsonValue in dsonArray) {
            writeDsonValue(writer, dsonValue, default);
        }
        writer.WriteEndArray();
    }

    public static DsonArray<TName> readArray<TName>(IDsonReader<TName> reader) where TName : IEquatable<TName> {
        DsonArray<TName> dsonArray = new DsonArray<TName>();
        DsonType dsonType;
        DsonValue value;
        reader.ReadStartArray();
        while ((dsonType = reader.ReadDsonType()) != DsonType.END_OF_OBJECT) {
            if (dsonType == DsonType.HEADER) {
                readHeader(reader, dsonArray.Header);
            }
            else {
                value = readDsonValue(reader);
                dsonArray.Add(value);
            }
        }
        reader.ReadEndArray();
        return dsonArray;
    }

    public static void writeHeader<TName>(IDsonWriter<TName> writer, DsonHeader<TName> header) where TName : IEquatable<TName> {
        if (header.Count == 1 && typeof(TName) == typeof(string)) {
            if (header.AsHeader<string>().TryGetValue(DsonHeaderFields.NAMES_CLASS_NAME, out DsonValue clsName)) {
                writer.WriteSimpleHeader(clsName.AsString()); // header只包含clsName时打印为简单模式
                return;
            }
        }
        writer.WriteStartHeader();
        foreach (var pair in header) {
            writeDsonValue(writer, pair.Value, pair.Key);
        }
        writer.WriteEndHeader();
    }

    public static DsonHeader<TName> readHeader<TName>(IDsonReader<TName> reader, DsonHeader<TName>? header) where TName : IEquatable<TName> {
        if (header == null) header = new DsonHeader<TName>();
        DsonType dsonType;
        TName name;
        DsonValue value;
        reader.ReadStartHeader();
        while ((dsonType = reader.ReadDsonType()) != DsonType.END_OF_OBJECT) {
            Debug.Assert(dsonType != DsonType.HEADER);
            name = reader.ReadName();
            value = readDsonValue(reader);
            header[name] = value;
        }
        reader.ReadEndHeader();
        return header;
    }

    public static void writeDsonValue<TName>(IDsonWriter<TName> writer, DsonValue dsonValue, TName? name) where TName : IEquatable<TName> {
        if (writer.IsAtName) {
            writer.WriteName(name);
        }
        switch (dsonValue.DsonType) {
            case DsonType.INT32:
                writer.WriteInt32(name, dsonValue.AsInt32(), WireType.VarInt, NumberStyles.Typed); // 必须能精确反序列化
                break;
            case DsonType.INT64:
                writer.WriteInt64(name, dsonValue.AsInt64(), WireType.VarInt, NumberStyles.Typed);
                break;
            case DsonType.FLOAT:
                writer.WriteFloat(name, dsonValue.AsFloat(), NumberStyles.Typed);
                break;
            case DsonType.DOUBLE:
                writer.WriteDouble(name, dsonValue.AsDouble(), NumberStyles.Simple);
                break;
            case DsonType.BOOLEAN:
                writer.WriteBoolean(name, dsonValue.AsBool());
                break;
            case DsonType.STRING:
                writer.WriteString(name, dsonValue.AsString(), StringStyle.Auto);
                break;
            case DsonType.NULL:
                writer.WriteNull(name);
                break;
            case DsonType.BINARY:
                writer.WriteBinary(name, dsonValue.AsBinary());
                break;
            case DsonType.EXT_INT32:
                writer.WriteExtInt32(name, dsonValue.AsExtInt32(), WireType.VarInt, NumberStyles.Simple);
                break;
            case DsonType.EXT_INT64:
                writer.WriteExtInt64(name, dsonValue.AsExtInt64(), WireType.VarInt, NumberStyles.Simple);
                break;
            case DsonType.EXT_DOUBLE:
                writer.WriteExtDouble(name, dsonValue.AsExtDouble(), NumberStyles.Simple);
                break;
            case DsonType.EXT_STRING:
                writer.WriteExtString(name, dsonValue.AsExtString(), StringStyle.Auto);
                break;
            case DsonType.REFERENCE:
                writer.WriteRef(name, dsonValue.AsReference());
                break;
            case DsonType.TIMESTAMP:
                writer.WriteTimestamp(name, dsonValue.AsTimestamp());
                break;
            case DsonType.HEADER:
                writeHeader(writer, dsonValue.AsHeader<TName>());
                break;
            case DsonType.ARRAY:
                writeArray(writer, dsonValue.AsArray<TName>(), ObjectStyle.Indent);
                break;
            case DsonType.OBJECT:
                writeObject(writer, dsonValue.AsObject<TName>(), ObjectStyle.Indent);
                break;
            case DsonType.END_OF_OBJECT:
            default:
                throw new InvalidOperationException();
        }
    }

    public static DsonValue readDsonValue<TName>(IDsonReader<TName> reader) where TName : IEquatable<TName> {
        DsonType dsonType = reader.CurrentDsonType;
        reader.SkipName();
        TName name = default;
        switch (dsonType) {
            case DsonType.INT32: return new DsonInt32(reader.ReadInt32(name));
            case DsonType.INT64: return new DsonInt64(reader.ReadInt64(name));
            case DsonType.FLOAT: return new DsonFloat(reader.ReadFloat(name));
            case DsonType.DOUBLE: return new DsonDouble(reader.ReadDouble(name));
            case DsonType.BOOLEAN: return new DsonBool(reader.ReadBoolean(name));
            case DsonType.STRING: return new DsonString(reader.ReadString(name));
            case DsonType.NULL: {
                reader.ReadNull(name);
                return DsonNull.Null;
            }
            case DsonType.BINARY: return reader.ReadBinary(name);
            case DsonType.EXT_INT32: return reader.ReadExtInt32(name);
            case DsonType.EXT_INT64: return reader.ReadExtInt64(name);
            case DsonType.EXT_DOUBLE: return reader.ReadExtDouble(name);
            case DsonType.EXT_STRING: return reader.ReadExtString(name);
            case DsonType.REFERENCE: return new DsonReference(reader.ReadRef(name));
            case DsonType.TIMESTAMP: return new DsonTimestamp(reader.ReadTimestamp(name));
            case DsonType.HEADER: {
                DsonHeader<TName> header = new DsonHeader<TName>();
                readHeader(reader, header);
                return header;
            }
            case DsonType.OBJECT: return readObject(reader);
            case DsonType.ARRAY: return readArray(reader);
            case DsonType.END_OF_OBJECT:
            default: throw new InvalidOperationException();
        }
    }

    #endregion

    #region 工厂方法

    public static DsonScanner NewJsonScanner(string jsonString) {
        return new DsonScanner(DsonCharStream.NewCharStream(jsonString, DsonMode.RELAXED));
    }

    public static DsonScanner NewStringScanner(string dsonString, DsonMode dsonMode = DsonMode.STANDARD) {
        return new DsonScanner(DsonCharStream.NewCharStream(dsonString, dsonMode));
    }

    public static DsonScanner NewStreamScanner(StreamReader reader, DsonMode dsonMode = DsonMode.STANDARD,
                                               int bufferSize = 512, bool autoClose = true) {
        return new DsonScanner(DsonCharStream.NewBufferedCharStream(reader, dsonMode, bufferSize, autoClose));
    }

    #endregion
}