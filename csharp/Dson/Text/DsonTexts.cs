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

using System.Collections;
using System.Collections.Immutable;
using System.Text;

namespace Dson.Text;

public static class DsonTexts
{
    // 类型标签
    public const string LABEL_INT32 = "i";
    public const string LABEL_INT64 = "L";
    public const string LABEL_FLOAT = "f";
    public const string LABEL_DOUBLE = "d";
    public const string LABEL_BOOL = "b";
    public const string LABEL_STRING = "s";
    public const string LABEL_NULL = "N";
    /** 长文本，字符串不需要加引号，不对内容进行转义，可直接换行 */
    public const string LABEL_TEXT = "ss";

    public const string LABEL_BINARY = "bin";
    public const string LABEL_EXTINT32 = "ei";
    public const string LABEL_EXTINT64 = "eL";
    public const string LABEL_EXTDOUBLE = "ed";
    public const string LABEL_EXTSTRING = "es";
    public const string LABEL_REFERENCE = "ref";
    public const string LABEL_DATETIME = "dt";

    public const string LABEL_BEGIN_OBJECT = "{";
    public const string LABEL_END_OBJECT = "}";
    public const string LABEL_BEGIN_ARRAY = "[";
    public const string LABEL_END_ARRAY = "]";
    public const string LABEL_BEGIN_HEADER = "@{";

    // 行首标签
    public const string HEAD_COMMENT = "#";
    public const string HEAD_APPEND_LINE = "-";
    public const string HEAD_APPEND = "|";
    public const string HEAD_SWITCH_MODE = "^";
    public const string HEAD_END_OF_TEXT = "~";

    /** 有特殊含义的字符串 */
    private static readonly ISet<string> PARSABLE_STRINGS = new[] {
        "true", "false",
        "null", "undefine",
        "NaN", "Infinity", "-Infinity"
    }.ToImmutableHashSet();

    private static readonly char[] DIGITS_UPPER = {
        '0', '1', '2', '3', '4', '5', '6', '7', '8', '9',
        'A', 'B', 'C', 'D', 'E', 'F'
    };

    /**
     * 规定哪些不安全较为容易，规定哪些安全反而不容易
     * 这些字符都是128内，使用bitset很快，还可以避免第三方依赖
     */
    private static readonly BitArray unsafeCharSet = new BitArray(128);

    static DsonTexts() {
        char[] tokenCharArray = "{}[],:\"@\\".ToCharArray();
        char[] reservedCharArray = "()".ToCharArray();
        foreach (char c in tokenCharArray) {
            unsafeCharSet.Set(c, true);
        }
        foreach (char c in reservedCharArray) {
            unsafeCharSet.Set(c, true);
        }
    }

    /** 是否是缩进字符 */
    public static bool isIndentChar(int c) {
        return c == ' ' || c == '\t';
    }

    /** 是否是不安全的字符，不能省略引号的字符 */
    public static bool isUnsafeStringChar(int c) {
        if (c < 128) { // BitArray不能访问索引外的字符
            return unsafeCharSet.Get(c) || char.IsWhiteSpace((char)c);
        }
        return char.IsWhiteSpace((char)c);
    }

    /**
     * 是否是安全字符，可以省略引号的字符
     * 注意：safeChar也可能组合出不安全的无引号字符串，比如：123, 0.5, null,true,false，
     * 因此不能因为每个字符安全，就认为整个字符串安全
     */
    public static bool isSafeStringChar(int c) {
        if (c < 128) {
            return !unsafeCharSet.Get(c) && !char.IsWhiteSpace((char)c);
        }
        return !char.IsWhiteSpace((char)c);
    }

    /**
     * 是否可省略字符串的引号
     * 其实并不建议底层默认判断是否可以不加引号，用户可以根据自己的数据决定是否加引号，比如；guid可能就是可以不加引号的
     * 这里的计算是保守的，保守一些不容易出错，因为情况太多，否则既难以保证正确性，性能也差
     */
    public static bool canUnquoteString(string value, int maxLengthOfUnquoteString) {
        if (value.Length == 0 || value.Length > maxLengthOfUnquoteString) {
            return false; // 长字符串都加引号，避免不必要的计算
        }
        if (PARSABLE_STRINGS.Contains(value)) {
            return false; // 特殊字符串值
        }
        // 这遍历的不是unicode码点，但不影响
        for (int i = 0; i < value.Length; i++) {
            char c = value[i];
            if (isUnsafeStringChar(c)) {
                return false;
            }
        }
        // 是否是可解析的数字类型，这个开销大放最后检测
        if (isParsable(value)) {
            return false;
        }
        return true;
    }

    /** 是否是ASCII码中的可打印字符构成的文本 */
    public static bool isASCIIText(String text) {
        for (int i = 0, len = text.Length; i < len; i++) {
            char c = text[i];
            if (c < 32 || c > 126) {
                return false;
            }
        }
        return true;
    }

    public static bool parseBool(string str) {
        if (str == "true" || str == "1") return true;
        if (str == "false" || str == "0") return false;
        throw new ArgumentException("invalid bool str: " + str);
    }

    public static void checkNullString(String str) {
        if ("null" == str) {
            return;
        }
        throw new ArgumentException("invalid null str: " + str);
    }

    #region 数字

    /** 是否是可解析的数字类型 */
    public static bool isParsable(string str) {
        int length = str.Length;
        if (length == 0 || length > 67 + 16) {
            return false; // 最长也不应该比二进制格式长，16是下划线预留
        }
        // 其实在这里是不想让科学计数法成功的
        return long.TryParse(str, out long _)
               || double.TryParse(str, out double _);
    }

    public static int parseInt(in string rawStr) {
        string str = rawStr;
        if (str.IndexOf('_') >= 0) {
            str = deleteUnderline(str);
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
            return sign * Convert.ToInt32(str, 16); // 解析16进制时可带有0x
        }
        if (str.StartsWith("0b") || str.StartsWith("0B")) {
            str = str.Substring(2); // c#解析二进制时不能带有0b...
            return sign * Convert.ToInt32(str, 2);
        }
        return int.Parse(str);
    }

    public static long parseLong(in string rawStr) {
        string str = rawStr;
        if (str.IndexOf('_') >= 0) {
            str = deleteUnderline(str);
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
            return sign * Convert.ToInt64(str, 16);
        }
        if (str.StartsWith("0b") || str.StartsWith("0B")) {
            str = str.Substring(2);
            return sign * Convert.ToInt64(str, 2);
        }
        return long.Parse(str);
    }

    public static float parseFloat(in string rawStr) {
        string str = rawStr;
        if (str.IndexOf('_') >= 0) {
            str = deleteUnderline(str);
        }
        return float.Parse(str);
    }

    public static double parseDouble(in string rawStr) {
        string str = rawStr;
        if (str.IndexOf('_') >= 0) {
            str = deleteUnderline(str);
        }
        return double.Parse(str);
    }

    private static string deleteUnderline(in string str) {
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

    #region 字节数组

    #endregion

    /// <summary>
    /// 
    /// </summary>
    /// <param name="label">行首标签</param>
    /// <returns>行首值</returns>
    public static LineHead? LineHeadOfLabel(string label) {
        return label switch {
            DsonTexts.HEAD_COMMENT => LineHead.COMMENT,
            DsonTexts.HEAD_APPEND_LINE => LineHead.APPEND_LINE,
            DsonTexts.HEAD_APPEND => LineHead.APPEND,
            DsonTexts.HEAD_SWITCH_MODE => LineHead.SWITCH_MODE,
            DsonTexts.HEAD_END_OF_TEXT => LineHead.END_OF_TEXT,
            _ => null
        };
    }

    public static string GetLabel(LineHead lineHead) {
        return lineHead switch {
            LineHead.COMMENT => DsonTexts.HEAD_COMMENT,
            LineHead.APPEND_LINE => DsonTexts.HEAD_APPEND_LINE,
            LineHead.APPEND => DsonTexts.HEAD_APPEND,
            LineHead.SWITCH_MODE => DsonTexts.HEAD_SWITCH_MODE,
            LineHead.END_OF_TEXT => DsonTexts.HEAD_END_OF_TEXT,
            _ => throw new InvalidOperationException()
        };
    }

    public static DsonToken clsNameTokenOfType(DsonType dsonType) {
        return dsonType switch {
            DsonType.INT32 => new DsonToken(DsonTokenType.CLASS_NAME, LABEL_INT32, -1),
            DsonType.INT64 => new DsonToken(DsonTokenType.CLASS_NAME, LABEL_INT64, -1),
            DsonType.FLOAT => new DsonToken(DsonTokenType.CLASS_NAME, LABEL_FLOAT, -1),
            DsonType.DOUBLE => new DsonToken(DsonTokenType.CLASS_NAME, LABEL_DOUBLE, -1),
            DsonType.BOOLEAN => new DsonToken(DsonTokenType.CLASS_NAME, LABEL_BOOL, -1),
            DsonType.STRING => new DsonToken(DsonTokenType.CLASS_NAME, LABEL_STRING, -1),
            DsonType.NULL => new DsonToken(DsonTokenType.CLASS_NAME, LABEL_NULL, -1),
            DsonType.BINARY => new DsonToken(DsonTokenType.CLASS_NAME, LABEL_BINARY, -1),
            DsonType.EXT_INT32 => new DsonToken(DsonTokenType.CLASS_NAME, LABEL_EXTINT32, -1),
            DsonType.EXT_INT64 => new DsonToken(DsonTokenType.CLASS_NAME, LABEL_EXTINT64, -1),
            DsonType.EXT_DOUBLE => new DsonToken(DsonTokenType.CLASS_NAME, LABEL_EXTDOUBLE, -1),
            DsonType.EXT_STRING => new DsonToken(DsonTokenType.CLASS_NAME, LABEL_EXTSTRING, -1),
            DsonType.REFERENCE => new DsonToken(DsonTokenType.CLASS_NAME, LABEL_REFERENCE, -1),
            DsonType.TIMESTAMP => new DsonToken(DsonTokenType.CLASS_NAME, LABEL_DATETIME, -1),
            _ => throw new ArgumentException(nameof(dsonType))
        };
    }
}