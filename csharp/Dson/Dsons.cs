#region LICENSE

//  Copyright 2023 wjybxx
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

using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;
using Wjybxx.Dson.Text;

namespace Wjybxx.Dson;

/// <summary>
/// C#是真泛型，因此可以减少类型
/// </summary>
public static class Dsons
{
    #region 基础常量

    /** {@link DsonType}占用的比特位 */
    public const int DsonTypeBites = 5;
    /** {@link DsonType}的最大类型编号 */
    public const int DsonTypeMaxValue = 31;

    /** {@link WireType}占位的比特位数 */
    public const int WireTypeBits = 3;
    public const int WireTypeMask = (1 << WireTypeBits) - 1;
    /** wireType看做数值时的最大值 */
    public const int WireTypeMaxValue = 7;

    /** 完整类型信息占用的比特位数 */
    public const int FullTypeBits = DsonTypeBites + WireTypeBits;
    public const int FullTypeMask = (1 << FullTypeBits) - 1;

    /** 二进制数据的最大长度 -- type使用 varint编码，最大占用5个字节 */
    public const int MaxBinaryLength = int.MaxValue - 5;

    #endregion

    #region 二进制常量

    /** 继承深度占用的比特位 */
    private const int IdepBits = 3;
    private const int IdepMask = (1 << IdepBits) - 1;
    /**
     * 支持的最大继承深度 - 7
     * 1.idep的深度不包含Object，没有显式继承其它类的类，idep为0
     * 2.超过7层我认为是你的代码有问题，而不是框架问题
     */
    public const int IdepMaxValue = IdepMask;

    /** 类字段最大number */
    private const short LnumberMaxValue = 8191;
    /** 类字段占用的最大比特位数 - 暂不对外开放 */
    private const int LnumberMaxBits = 13;

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
        if (length > MaxBinaryLength) {
            throw new ArgumentException($"the length of data must between[0, {MaxBinaryLength}], but found: {length}");
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
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int MakeFullType(DsonType dsonType, WireType wireType) {
        return ((int)dsonType << WireTypeBits) | (int)wireType;
    }

    /// <summary>
    /// 用于非常规类型计算FullType
    /// </summary>
    /// <param name="dsonType">Dson数据类型 5bits[0~31]</param>
    /// <param name="wireType">特殊编码类型 3bits[0~7]</param>
    /// <returns>完整类型</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int MakeFullType(int dsonType, int wireType) {
        return (dsonType << WireTypeBits) | wireType;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int DsonTypeOfFullType(int fullType) {
        return DsonInternals.LogicalShiftRight(fullType, WireTypeBits);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int WireTypeOfFullType(int fullType) {
        return (fullType & WireTypeMask);
    }

    #endregion

    #region FieldNumber

    /// <summary>
    /// 计算字段的完整数字
    /// </summary>
    /// <param name="idep">继承深度[0~7]</param>
    /// <param name="lnumber">字段在类本地的编号</param>
    /// <returns>字段的完整编号</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int MakeFullNumber(int idep, int lnumber) {
        return (lnumber << IdepBits) | idep;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int LnumberOfFullNumber(int fullNumber) {
        return DsonInternals.LogicalShiftRight(fullNumber, IdepBits);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static byte IdepOfFullNumber(int fullNumber) {
        return (byte)(fullNumber & IdepMask);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int MakeFullNumberZeroIdep(int lnumber) {
        return lnumber << IdepBits;
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

    public static void WriteTopDsonValue<TName>(IDsonWriter<TName> writer, DsonValue dsonValue,
                                                ObjectStyle style = ObjectStyle.Indent) where TName : IEquatable<TName> {
        if (dsonValue.DsonType == DsonType.Object) {
            WriteObject(writer, dsonValue.AsObject<TName>(), style);
        }
        else if (dsonValue.DsonType == DsonType.Array) {
            WriteArray(writer, dsonValue.AsArray<TName>(), style);
        }
        else {
            WriteHeader(writer, dsonValue.AsHeader<TName>());
        }
    }

    /** @return 如果到达文件尾部，则返回null */
    public static DsonValue? ReadTopDsonValue<TName>(IDsonReader<TName> reader) where TName : IEquatable<TName> {
        DsonType dsonType = reader.ReadDsonType();
        if (dsonType == DsonType.EndOfObject) {
            return null;
        }
        if (dsonType == DsonType.Object) {
            return ReadObject(reader);
        }
        else if (dsonType == DsonType.Array) {
            return ReadArray(reader);
        }
        else {
            Debug.Assert(dsonType == DsonType.Header);
            return ReadHeader(reader, new DsonHeader<TName>());
        }
    }

    /** 如果需要写入名字，外部写入 */
    public static void WriteObject<TName>(IDsonWriter<TName> writer, DsonObject<TName> dsonObject,
                                          ObjectStyle style = ObjectStyle.Indent) where TName : IEquatable<TName> {
        writer.WriteStartObject(style);
        if (dsonObject.Header.Count > 0) {
            WriteHeader(writer, dsonObject.Header);
        }
        foreach (var pair in dsonObject) {
            WriteDsonValue(writer, pair.Value, pair.Key);
        }
        writer.WriteEndObject();
    }

    public static DsonObject<TName> ReadObject<TName>(IDsonReader<TName> reader) where TName : IEquatable<TName> {
        DsonObject<TName> dsonObject = new DsonObject<TName>();
        DsonType dsonType;
        TName name;
        DsonValue value;
        reader.ReadStartObject();
        while ((dsonType = reader.ReadDsonType()) != DsonType.EndOfObject) {
            if (dsonType == DsonType.Header) {
                ReadHeader(reader, dsonObject.Header);
            }
            else {
                name = reader.ReadName();
                value = ReadDsonValue(reader);
                dsonObject[name] = value;
            }
        }
        reader.ReadEndObject();
        return dsonObject;
    }

    /** 如果需要写入名字，外部写入 */
    public static void WriteArray<TName>(IDsonWriter<TName> writer, DsonArray<TName> dsonArray,
                                         ObjectStyle style = ObjectStyle.Indent) where TName : IEquatable<TName> {
        writer.WriteStartArray(style);
        if (dsonArray.Header.Count > 0) {
            WriteHeader(writer, dsonArray.Header);
        }
        foreach (DsonValue dsonValue in dsonArray) {
            WriteDsonValue(writer, dsonValue, default);
        }
        writer.WriteEndArray();
    }

    public static DsonArray<TName> ReadArray<TName>(IDsonReader<TName> reader) where TName : IEquatable<TName> {
        DsonArray<TName> dsonArray = new DsonArray<TName>();
        DsonType dsonType;
        DsonValue value;
        reader.ReadStartArray();
        while ((dsonType = reader.ReadDsonType()) != DsonType.EndOfObject) {
            if (dsonType == DsonType.Header) {
                ReadHeader(reader, dsonArray.Header);
            }
            else {
                value = ReadDsonValue(reader);
                dsonArray.Add(value);
            }
        }
        reader.ReadEndArray();
        return dsonArray;
    }

    public static void WriteHeader<TName>(IDsonWriter<TName> writer, DsonHeader<TName> header) where TName : IEquatable<TName> {
        if (header.Count == 1 && typeof(TName) == typeof(string)) {
            if (header.AsHeader<string>().TryGetValue(DsonHeaders.NamesClassName, out DsonValue clsName)) {
                writer.WriteSimpleHeader(clsName.AsString()); // header只包含clsName时打印为简单模式
                return;
            }
        }
        writer.WriteStartHeader();
        foreach (var pair in header) {
            WriteDsonValue(writer, pair.Value, pair.Key);
        }
        writer.WriteEndHeader();
    }

    public static DsonHeader<TName> ReadHeader<TName>(IDsonReader<TName> reader, DsonHeader<TName>? header) where TName : IEquatable<TName> {
        if (header == null) header = new DsonHeader<TName>();
        DsonType dsonType;
        TName name;
        DsonValue value;
        reader.ReadStartHeader();
        while ((dsonType = reader.ReadDsonType()) != DsonType.EndOfObject) {
            Debug.Assert(dsonType != DsonType.Header);
            name = reader.ReadName();
            value = ReadDsonValue(reader);
            header[name] = value;
        }
        reader.ReadEndHeader();
        return header;
    }

    public static void WriteDsonValue<TName>(IDsonWriter<TName> writer, DsonValue dsonValue, TName? name) where TName : IEquatable<TName> {
        if (writer.IsAtName) {
            writer.WriteName(name);
        }
        switch (dsonValue.DsonType) {
            case DsonType.Int32:
                writer.WriteInt32(name, dsonValue.AsInt32(), WireType.VarInt, NumberStyles.Typed); // 必须能精确反序列化
                break;
            case DsonType.Int64:
                writer.WriteInt64(name, dsonValue.AsInt64(), WireType.VarInt, NumberStyles.Typed);
                break;
            case DsonType.Float:
                writer.WriteFloat(name, dsonValue.AsFloat(), NumberStyles.Typed);
                break;
            case DsonType.Double:
                writer.WriteDouble(name, dsonValue.AsDouble(), NumberStyles.Simple);
                break;
            case DsonType.Boolean:
                writer.WriteBoolean(name, dsonValue.AsBool());
                break;
            case DsonType.String:
                writer.WriteString(name, dsonValue.AsString());
                break;
            case DsonType.Null:
                writer.WriteNull(name);
                break;
            case DsonType.Binary:
                writer.WriteBinary(name, dsonValue.AsBinary());
                break;
            case DsonType.ExtInt32:
                writer.WriteExtInt32(name, dsonValue.AsExtInt32(), WireType.VarInt, NumberStyles.Simple);
                break;
            case DsonType.ExtInt64:
                writer.WriteExtInt64(name, dsonValue.AsExtInt64(), WireType.VarInt, NumberStyles.Simple);
                break;
            case DsonType.ExtDouble:
                writer.WriteExtDouble(name, dsonValue.AsExtDouble(), NumberStyles.Simple);
                break;
            case DsonType.ExtString:
                writer.WriteExtString(name, dsonValue.AsExtString());
                break;
            case DsonType.Reference:
                writer.WriteRef(name, dsonValue.AsReference());
                break;
            case DsonType.Timestamp:
                writer.WriteTimestamp(name, dsonValue.AsTimestamp());
                break;
            case DsonType.Header:
                WriteHeader(writer, dsonValue.AsHeader<TName>());
                break;
            case DsonType.Array:
                WriteArray(writer, dsonValue.AsArray<TName>(), ObjectStyle.Indent);
                break;
            case DsonType.Object:
                WriteObject(writer, dsonValue.AsObject<TName>(), ObjectStyle.Indent);
                break;
            case DsonType.EndOfObject:
            default:
                throw new InvalidOperationException();
        }
    }

    public static DsonValue ReadDsonValue<TName>(IDsonReader<TName> reader) where TName : IEquatable<TName> {
        DsonType dsonType = reader.CurrentDsonType;
        reader.SkipName();
        TName name = default;
        switch (dsonType) {
            case DsonType.Int32: return new DsonInt32(reader.ReadInt32(name));
            case DsonType.Int64: return new DsonInt64(reader.ReadInt64(name));
            case DsonType.Float: return new DsonFloat(reader.ReadFloat(name));
            case DsonType.Double: return new DsonDouble(reader.ReadDouble(name));
            case DsonType.Boolean: return new DsonBool(reader.ReadBoolean(name));
            case DsonType.String: return new DsonString(reader.ReadString(name));
            case DsonType.Null: {
                reader.ReadNull(name);
                return DsonNull.Null;
            }
            case DsonType.Binary: return reader.ReadBinary(name);
            case DsonType.ExtInt32: return reader.ReadExtInt32(name);
            case DsonType.ExtInt64: return reader.ReadExtInt64(name);
            case DsonType.ExtDouble: return reader.ReadExtDouble(name);
            case DsonType.ExtString: return reader.ReadExtString(name);
            case DsonType.Reference: return new DsonReference(reader.ReadRef(name));
            case DsonType.Timestamp: return new DsonTimestamp(reader.ReadTimestamp(name));
            case DsonType.Header: {
                DsonHeader<TName> header = new DsonHeader<TName>();
                ReadHeader(reader, header);
                return header;
            }
            case DsonType.Object: return ReadObject(reader);
            case DsonType.Array: return ReadArray(reader);
            case DsonType.EndOfObject:
            default: throw new InvalidOperationException();
        }
    }

    #endregion

    #region 快捷方法

    public static string ToDson(DsonValue dsonValue, ObjectStyle style, DsonMode dsonMode = DsonMode.Standard) {
        return ToDson(dsonValue, style, dsonMode == DsonMode.Relaxed
            ? DsonTextWriterSettings.RelaxedDefault
            : DsonTextWriterSettings.Default);
    }

    public static string ToDson(DsonValue dsonValue, ObjectStyle style, DsonTextWriterSettings settings) {
        if (!dsonValue.DsonType.IsContainerOrHeader()) {
            throw new InvalidOperationException("invalid dsonType " + dsonValue.DsonType);
        }
        StringWriter stringWriter = new StringWriter(new StringBuilder(1024));
        using (DsonTextWriter writer = new DsonTextWriter(settings, stringWriter)) {
            WriteTopDsonValue(writer, dsonValue, style);
        }
        return stringWriter.ToString();
    }

    public static DsonValue FromDson(string dsonString) {
        using (DsonTextReader reader = new DsonTextReader(DsonTextReaderSettings.Default, dsonString)) {
            return ReadTopDsonValue(reader)!;
        }
    }

    /** @param jsonString json字符串或无行首的dson字符串 */
    public static DsonValue FromJson(string jsonString) {
        using (DsonTextReader reader = new DsonTextReader(DsonTextReaderSettings.Default, NewJsonScanner(jsonString))) {
            return ReadTopDsonValue(reader)!;
        }
    }

    /** 获取dsonValue的localId -- dson的约定之一 */
    public static string? GetLocalId(DsonValue dsonValue) {
        DsonHeader<string> header;
        if (dsonValue is DsonObject<string> dsonObject) {
            header = dsonObject.Header;
        }
        else if (dsonValue is DsonArray<string> dsonArray) {
            header = dsonArray.Header;
        }
        else {
            return null;
        }
        if (header.TryGetValue(DsonHeaders.NamesLocalId, out DsonValue wrapped)) {
            return wrapped is DsonString dsonString ? dsonString.Value : null;
        }
        return null;
    }

    #endregion

    #region 工厂方法

    public static DsonScanner NewJsonScanner(string jsonString) {
        return new DsonScanner(IDsonCharStream.NewCharStream(jsonString, DsonMode.Relaxed));
    }

    public static DsonScanner NewStringScanner(string dsonString, DsonMode dsonMode = DsonMode.Standard) {
        return new DsonScanner(IDsonCharStream.NewCharStream(dsonString, dsonMode));
    }

    public static DsonScanner NewStreamScanner(TextReader reader, DsonMode dsonMode = DsonMode.Standard,
                                               int bufferSize = 512, bool autoClose = true) {
        return new DsonScanner(IDsonCharStream.NewBufferedCharStream(reader, dsonMode, bufferSize, autoClose));
    }

    #endregion
}