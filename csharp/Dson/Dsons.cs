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
}