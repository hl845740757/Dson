#region LICENSE

//  Copyright 2023 wjybxx(845740757@qq.com)
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

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;
using Wjybxx.Dson.IO;

#pragma warning disable CS1591
namespace Wjybxx.Dson.Text;

/// <summary>
/// Dson文本解析工具类
/// </summary>
public static class DsonTexts
{
    // 类型标签
    public const string LabelInt32 = "i";
    public const string LabelInt64 = "L";
    public const string LabelFloat = "f";
    public const string LabelDouble = "d";
    public const string LabelBool = "b";
    public const string LabelString = "s";
    public const string LabelNull = "N";
    
    /** 多行纯文本，字符串不需要加引号，不对内容进行转义，可直接换行 */
    public const string LabelText = "ss";
    /** 单行纯文本，字符串不需要加引号，不对内容进行转义 */
    public const string LabelStringLine = "sL";
    /** 注释/文档 -- 简单的单行纯文本 */
    public const string LabelDoc = "doc";

    public const string LabelBinary = "bin";
    public const string LabelExtInt32 = "ei";
    public const string LabelExtInt64 = "eL";
    public const string LabelExtDouble = "ed";
    public const string LabelExtString = "es";
    public const string LabelReference = "ref";
    public const string LabelDatetime = "dt";

    public const string LabelBeginObject = "{";
    public const string LabelEndObject = "}";
    public const string LabelBeginArray = "[";
    public const string LabelEndArray = "]";
    public const string LabelBeginHeader = "@{";

    /** 所有内建值类型标签 */
    public static readonly ISet<string> BuiltinStructLabels = new[]
    {
        LabelBinary, LabelExtInt32, LabelExtInt64, LabelExtDouble, LabelExtString,
        LabelReference, LabelDatetime
    }.ToImmutableHashSet();

    // 行首标签
    public const string HeadComment = "#";
    public const string HeadAppendLine = "-";
    public const string HeadAppend = "|";
    public const string HeadSwitchMode = "^";
    public const string HeadEndOfText = "~";

    /** 有特殊含义的字符串 */
    private static readonly ISet<string> ParsableStrings = new[]
    {
        "true", "false",
        "null", "undefine",
        "NaN", "Infinity", "-Infinity"
    }.ToImmutableHashSet();

    /**
     * 规定哪些不安全较为容易，规定哪些安全反而不容易
     * 这些字符都是128内，使用bitset很快，还可以避免第三方依赖
     */
    private static readonly BitArray UnsafeCharSet = new BitArray(128);

    static DsonTexts() {
        char[] tokenCharArray = "{}[],:\"@\\".ToCharArray();
        char[] reservedCharArray = "()".ToCharArray();
        foreach (char c in tokenCharArray) {
            UnsafeCharSet.Set(c, true);
        }
        foreach (char c in reservedCharArray) {
            UnsafeCharSet.Set(c, true);
        }
    }

    /** 是否是缩进字符 */
    public static bool IsIndentChar(int c) {
        return c == ' ' || c == '\t';
    }

    /** 是否是不安全的字符，不能省略引号的字符 */
    public static bool IsUnsafeStringChar(int c) {
        if (c < 128) { // BitArray不能访问索引外的字符
            return UnsafeCharSet.Get(c) || char.IsWhiteSpace((char)c);
        }
        return char.IsWhiteSpace((char)c);
    }

    /**
     * 是否是安全字符，可以省略引号的字符
     * 注意：safeChar也可能组合出不安全的无引号字符串，比如：123, 0.5, null,true,false，
     * 因此不能因为每个字符安全，就认为整个字符串安全
     */
    public static bool IsSafeStringChar(int c) {
        if (c < 128) {
            return !UnsafeCharSet.Get(c) && !char.IsWhiteSpace((char)c);
        }
        return !char.IsWhiteSpace((char)c);
    }

    /**
     * 是否可省略字符串的引号
     * 其实并不建议底层默认判断是否可以不加引号，用户可以根据自己的数据决定是否加引号，比如；guid可能就是可以不加引号的
     * 这里的计算是保守的，保守一些不容易出错，因为情况太多，否则既难以保证正确性，性能也差
     */
    public static bool CanUnquoteString(string value, int maxLengthOfUnquoteString) {
        if (value.Length == 0 || value.Length > maxLengthOfUnquoteString) {
            return false; // 长字符串都加引号，避免不必要的计算
        }
        if (ParsableStrings.Contains(value)) {
            return false; // 特殊字符串值
        }
        // 这遍历的不是unicode码点，但不影响
        for (int i = 0; i < value.Length; i++) {
            char c = value[i];
            if (IsUnsafeStringChar(c)) {
                return false;
            }
        }
        // 是否是可解析的数字类型，这个开销大放最后检测
        if (IsParsable(value)) {
            return false;
        }
        return true;
    }

    /** 是否是ASCII码中的可打印字符构成的文本 */
    public static bool IsAsciiText(string text) {
        for (int i = 0, len = text.Length; i < len; i++) {
            char c = text[i];
            if (c < 32 || c > 126) {
                return false;
            }
        }
        return true;
    }

    #region bool/null

    public static bool ParseBool(string str) {
        if (str == "true" || str == "1") return true;
        if (str == "false" || str == "0") return false;
        throw new ArgumentException("invalid bool str: " + str);
    }

    public static void CheckNullString(string str) {
        if ("null" == str) {
            return;
        }
        throw new ArgumentException("invalid null str: " + str);
    }
    #endregion

    #region 数字

    /** 是否是可解析的数字类型 */
    public static bool IsParsable(string str) {
        int length = str.Length;
        if (length == 0 || length > 67 + 16) {
            return false; // 最长也不应该比二进制格式长，16是下划线预留
        }
        return CommonsLang3.IsParsable(str);
    }

    public static int ParseInt(in string rawStr) {
        string str = rawStr;
        if (str.IndexOf('_') >= 0) {
            str = DeleteUnderline(str);
        }
        if (str.Length == 0) {
            throw new ArgumentException("empty string");
        }
        int lookOffset;
        int sign;
        char firstChar = str[0];
        if (firstChar == '+') {
            sign = 1;
            lookOffset = 1;
        } else if (firstChar == '-') {
            sign = -1;
            lookOffset = 1;
        } else {
            sign = 1;
            lookOffset = 0;
        }
        if (lookOffset > 0) {
            str = str.Substring(lookOffset); // 跳过符号位
        }
        if (str.StartsWith("0x") || str.StartsWith("0X")) {
            return (int)(sign * Convert.ToUInt32(str, 16)); // 解析16进制时可带有0x
        }
        if (str.StartsWith("0b") || str.StartsWith("0B")) {
            str = str.Substring(2); // c#解析二进制时不能带有0b...
            return (int)(sign * Convert.ToUInt32(str, 2));
        }
        return int.Parse(str);
    }

    public static long ParseLong(in string rawStr) {
        string str = rawStr;
        if (str.IndexOf('_') >= 0) {
            str = DeleteUnderline(str);
        }
        if (str.Length == 0) {
            throw new ArgumentException("empty string");
        }
        int lookOffset;
        int sign;
        char firstChar = str[0];
        if (firstChar == '+') {
            sign = 1;
            lookOffset = 1;
        } else if (firstChar == '-') {
            sign = -1;
            lookOffset = 1;
        } else {
            sign = 1;
            lookOffset = 0;
        }
        if (lookOffset > 0) {
            str = str.Substring(lookOffset);
        }
        if (str.StartsWith("0x") || str.StartsWith("0X")) {
            return sign * (int)Convert.ToUInt64(str, 16);
        }
        if (str.StartsWith("0b") || str.StartsWith("0B")) {
            str = str.Substring(2);
            return sign * (int)Convert.ToUInt64(str, 2);
        }
        return long.Parse(str);
    }

    public static float ParseFloat(in string rawStr) {
        string str = rawStr;
        if (str.IndexOf('_') >= 0) {
            str = DeleteUnderline(str);
        }
        return float.Parse(str);
    }

    public static double ParseDouble(in string rawStr) {
        string str = rawStr;
        if (str.IndexOf('_') >= 0) {
            str = DeleteUnderline(str);
        }
        return double.Parse(str);
    }

    private static string DeleteUnderline(in string str) {
        int length = str.Length;
        if (str[0] == '_' || str[length - 1] == '_') {
            throw new ArgumentException(str); // 首尾不能下划线
        }
        StringBuilder sb = new StringBuilder(length - 1);
        bool hasUnderline = false;
        for (int i = 0; i < length; i++) {
            char c = str[i];
            if (c == '_') {
                if (hasUnderline) {
                    throw new ArgumentException(str); // 不能多个连续下划线
                }
                hasUnderline = true;
            } else {
                sb.Append(c);
                hasUnderline = false;
            }
        }
        return sb.ToString();
    }

    #endregion

    #region linehead扩展

    /** 通过行首label字符查找行首枚举 */
    public static LineHead? LineHeadOfLabel(string label) {
        return label switch
        {
            DsonTexts.HeadComment => LineHead.Comment,
            DsonTexts.HeadAppendLine => LineHead.AppendLine,
            DsonTexts.HeadAppend => LineHead.Append,
            DsonTexts.HeadSwitchMode => LineHead.SwitchMode,
            DsonTexts.HeadEndOfText => LineHead.EndOfText,
            _ => null
        };
    }

    /** 获取行首枚举关联的字符  */
    public static string GetLabel(LineHead lineHead) {
        return lineHead switch
        {
            LineHead.Comment => DsonTexts.HeadComment,
            LineHead.AppendLine => DsonTexts.HeadAppendLine,
            LineHead.Append => DsonTexts.HeadAppend,
            LineHead.SwitchMode => DsonTexts.HeadSwitchMode,
            LineHead.EndOfText => DsonTexts.HeadEndOfText,
            _ => throw new InvalidOperationException()
        };
    }
    #endregion

    /** 获取类型名对应的Token类型 */
    public static DsonTokenType TokenTypeOfClsName(string label) {
        if (label == null) throw new ArgumentNullException(nameof(label));
        return label switch
        {
            LabelInt32 => DsonTokenType.Int32,
            LabelInt64 => DsonTokenType.Int64,
            LabelFloat => DsonTokenType.Float,
            LabelDouble => DsonTokenType.Double,
            LabelBool => DsonTokenType.Bool,
            LabelString => DsonTokenType.String,
            LabelExtString => DsonTokenType.String,
            LabelStringLine => DsonTokenType.String,
            LabelDoc => DsonTokenType.Doc,
            LabelNull => DsonTokenType.Null,
            _ => BuiltinStructLabels.Contains(label) ? DsonTokenType.BuiltinStruct : DsonTokenType.SimpleHeader
        };
    }

    /** 获取dsonType关联的无位置Token */
    public static DsonToken ClsNameTokenOfType(DsonType dsonType) {
        return dsonType switch
        {
            DsonType.Int32 => new DsonToken(DsonTokenType.Int32, LabelInt32, -1),
            DsonType.Int64 => new DsonToken(DsonTokenType.Int64, LabelInt64, -1),
            DsonType.Float => new DsonToken(DsonTokenType.Float, LabelFloat, -1),
            DsonType.Double => new DsonToken(DsonTokenType.Double, LabelDouble, -1),
            DsonType.Boolean => new DsonToken(DsonTokenType.Bool, LabelBool, -1),
            DsonType.String => new DsonToken(DsonTokenType.String, LabelString, -1),
            DsonType.Null => new DsonToken(DsonTokenType.Null, LabelNull, -1),
            DsonType.Binary => new DsonToken(DsonTokenType.BuiltinStruct, LabelBinary, -1),
            DsonType.ExtInt32 => new DsonToken(DsonTokenType.BuiltinStruct, LabelExtInt32, -1),
            DsonType.ExtInt64 => new DsonToken(DsonTokenType.BuiltinStruct, LabelExtInt64, -1),
            DsonType.ExtDouble => new DsonToken(DsonTokenType.BuiltinStruct, LabelExtDouble, -1),
            DsonType.ExtString => new DsonToken(DsonTokenType.BuiltinStruct, LabelExtString, -1),
            DsonType.Reference => new DsonToken(DsonTokenType.BuiltinStruct, LabelReference, -1),
            DsonType.Timestamp => new DsonToken(DsonTokenType.BuiltinStruct, LabelDatetime, -1),
            _ => throw new ArgumentException(nameof(dsonType))
        };
    }

    /** 检测dson文本的类型 -- 对于文件，可以通过规范命名规范来表达 */
    public static DsonMode DetectDsonMode(string dsonString) {
        for (int i = 0, len = dsonString.Length; i < len; i++) {
            char firstChar = dsonString[i];
            if (char.IsWhiteSpace(firstChar)) {
                continue;
            }
            return DetectDsonMode(firstChar);
        }
        return DsonMode.Relaxed;
    }
    
    /**
     * 首个字符的范围：行首和对象开始符
     */
    public static DsonMode DetectDsonMode(char firstChar) {
        LineHead? lineHead = LineHeadOfLabel(firstChar.ToString());
        if (lineHead.HasValue) {
            return DsonMode.Standard;
        }
        if (firstChar != '{' && firstChar != '[' && firstChar != '@') {
            throw new ArgumentException("the string is not a valid dson string");
        }
        return DsonMode.Relaxed;
    }
    
    /** 索引首个非空白字符的位置 */
    public static int IndexOfNonWhitespace(string str, int startIndex) {
        if (startIndex < 0) {
            throw new IndexOutOfRangeException("Index out of range: " + startIndex);
        }
        for (int idx = startIndex, length = str.Length; idx < length; idx++) {
            if (!char.IsWhiteSpace(str[idx])) {
                return idx;
            }
        }
        return -1;
    }
}