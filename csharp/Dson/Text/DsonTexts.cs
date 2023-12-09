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

using System.Collections;
using System.Collections.Immutable;
using System.Text;

namespace Dson.Text;

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
    /** 长文本，字符串不需要加引号，不对内容进行转义，可直接换行 */
    public const string LabelText = "ss";

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

    // 行首标签
    public const string HeadComment = "#";
    public const string HeadAppendLine = "-";
    public const string HeadAppend = "|";
    public const string HeadSwitchMode = "^";
    public const string HeadEndOfText = "~";

    /** 有特殊含义的字符串 */
    private static readonly ISet<string> ParsableStrings = new[] {
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

    #region 数字

    /** 是否是可解析的数字类型 */
    public static bool IsParsable(string str) {
        int length = str.Length;
        if (length == 0 || length > 67 + 16) {
            return false; // 最长也不应该比二进制格式长，16是下划线预留
        }
        // 其实在这里是不想让科学计数法成功的
        return long.TryParse(str, out long _)
               || double.TryParse(str, out double _);
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
        }
        else if (firstChar == '-') {
            sign = -1;
            lookOffset = 1;
        }
        else {
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
        }
        else if (firstChar == '-') {
            sign = -1;
            lookOffset = 1;
        }
        else {
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
            }
            else {
                sb.Append(c);
                hasUnderline = false;
            }
        }
        return sb.ToString();
    }

    #endregion

    /// <summary>
    /// 
    /// </summary>
    /// <param name="label">行首标签</param>
    /// <returns>行首值</returns>
    public static LineHead? LineHeadOfLabel(string label) {
        return label switch {
            DsonTexts.HeadComment => LineHead.Comment,
            DsonTexts.HeadAppendLine => LineHead.AppendLine,
            DsonTexts.HeadAppend => LineHead.Append,
            DsonTexts.HeadSwitchMode => LineHead.SwitchMode,
            DsonTexts.HeadEndOfText => LineHead.EndOfText,
            _ => null
        };
    }

    public static string GetLabel(LineHead lineHead) {
        return lineHead switch {
            LineHead.Comment => DsonTexts.HeadComment,
            LineHead.AppendLine => DsonTexts.HeadAppendLine,
            LineHead.Append => DsonTexts.HeadAppend,
            LineHead.SwitchMode => DsonTexts.HeadSwitchMode,
            LineHead.EndOfText => DsonTexts.HeadEndOfText,
            _ => throw new InvalidOperationException()
        };
    }

    public static DsonToken ClsNameTokenOfType(DsonType dsonType) {
        return dsonType switch {
            DsonType.Int32 => new DsonToken(DsonTokenType.ClassName, LabelInt32, -1),
            DsonType.Int64 => new DsonToken(DsonTokenType.ClassName, LabelInt64, -1),
            DsonType.Float => new DsonToken(DsonTokenType.ClassName, LabelFloat, -1),
            DsonType.Double => new DsonToken(DsonTokenType.ClassName, LabelDouble, -1),
            DsonType.Boolean => new DsonToken(DsonTokenType.ClassName, LabelBool, -1),
            DsonType.String => new DsonToken(DsonTokenType.ClassName, LabelString, -1),
            DsonType.Null => new DsonToken(DsonTokenType.ClassName, LabelNull, -1),
            DsonType.Binary => new DsonToken(DsonTokenType.ClassName, LabelBinary, -1),
            DsonType.ExtInt32 => new DsonToken(DsonTokenType.ClassName, LabelExtInt32, -1),
            DsonType.ExtInt64 => new DsonToken(DsonTokenType.ClassName, LabelExtInt64, -1),
            DsonType.ExtDouble => new DsonToken(DsonTokenType.ClassName, LabelExtDouble, -1),
            DsonType.ExtString => new DsonToken(DsonTokenType.ClassName, LabelExtString, -1),
            DsonType.Reference => new DsonToken(DsonTokenType.ClassName, LabelReference, -1),
            DsonType.Timestamp => new DsonToken(DsonTokenType.ClassName, LabelDatetime, -1),
            _ => throw new ArgumentException(nameof(dsonType))
        };
    }
}