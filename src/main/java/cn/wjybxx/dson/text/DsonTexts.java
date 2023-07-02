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

package cn.wjybxx.dson.text;

import cn.wjybxx.dson.io.DsonIOException;
import org.apache.commons.lang3.math.NumberUtils;

import java.util.BitSet;
import java.util.Set;
import java.util.regex.Pattern;

/**
 * Dson的文本表示法
 * 类似json但不是json
 *
 * @author wjybxx
 * date - 2023/6/2
 */
public class DsonTexts {

    // 类型标签
    public static final String LABEL_INT32 = "i";
    public static final String LABEL_INT64 = "L";
    public static final String LABEL_FLOAT = "f";
    public static final String LABEL_DOUBLE = "d";
    public static final String LABEL_BOOL = "b";
    public static final String LABEL_STRING = "s";
    public static final String LABEL_NULL = "N";
    /** 长文本，字符串不需要加引号，不对内容进行转义，可直接换行 */
    public static final String LABEL_TEXT = "ss";

    public static final String LABEL_BINARY = "bin";
    public static final String LABEL_EXTINT32 = "ei";
    public static final String LABEL_EXTINT64 = "eL";
    public static final String LABEL_EXTSTRING = "es";
    public static final String LABEL_REFERENCE = "ref";
    public static final String LABEL_DATETIME = "dt";

    public static final String LABEL_BEGIN_OBJECT = "{";
    public static final String LABEL_END_OBJECT = "}";
    public static final String LABEL_BEGIN_ARRAY = "[";
    public static final String LABEL_END_ARRAY = "]";
    public static final String LABEL_BEGIN_HEADER = "@{";

    // 行首标签
    public static final String LHEAD_COMMENT = "#";
    public static final String LHEAD_APPEND_LINE = "-";
    public static final String LHEAD_APPEND = "|";
    public static final String LHEAD_SWITCH_MODE = "^";
    public static final String LHEAD_END_OF_TEXT = "~";
    public static final int CONTENT_LHEAD_LENGTH = 1;

    /** 有特殊含义的字符串 */
    private static final Set<String> PARSABLE_STRINGS = Set.of("true", "false",
            "null", "undefine",
            "NaN", "Infinity", "-Infinity");

    /**
     * 规定哪些不安全较为容易，规定哪些安全反而不容易
     * 这些字符都是128内，使用bitset很快，还可以避免第三方依赖
     */
    private static final BitSet unsafeCharSet = new BitSet(128);

    static {
        char[] tokenCharArray = "{}[],:\"@\\".toCharArray();
        char[] reservedCharArray = "()".toCharArray();
        for (char c : tokenCharArray) {
            unsafeCharSet.set(c);
        }
        for (char c : reservedCharArray) {
            unsafeCharSet.set(c);
        }
    }

    /** 是否是不安全的字符，不能省略引号的字符 */
    public static boolean isUnsafeStringChar(int c) {
        return unsafeCharSet.get(c) || Character.isWhitespace(c);
    }

    /**
     * 是否是安全字符，可以省略引号的字符
     * 注意：safeChar也可能组合出不安全的无引号字符串，比如：123, 0.5, null,true,false，
     * 因此不能因为每个字符安全，就认为整个字符串安全
     */
    public static boolean isSafeStringChar(int c) {
        return !unsafeCharSet.get(c) && !Character.isWhitespace(c);
    }

    /**
     * 是否可省略字符串的引号
     * 其实并不建议底层默认判断是否可以不加引号，用户可以根据自己的数据决定是否加引号，比如；guid可能就是可以不加引号的
     * 这里的计算是保守的，保守一些不容易出错，因为情况太多，否则既难以保证正确性，性能也差
     */
    public static boolean canUnquoteString(String value, int maxLengthOfUnquoteString) {
        if (value.isEmpty() || value.length() > maxLengthOfUnquoteString) { // 长字符串都加引号，避免不必要的计算
            return false;
        }
        if (PARSABLE_STRINGS.contains(value)) { // 特殊字符串值
            return false;
        }
        for (int i = 0; i < value.length(); i++) { // 这遍历的不是unicode码点，但不影响
            char c = value.charAt(i);
            if (isUnsafeStringChar(c)) {
                return false;
            }
        }
        if (isParsable(value)) { // 可解析的数字类型，这个开销大放最后检测
            return false;
        }
        return true;
    }

    /** 是否是ASCII码中的可打印字符构成的文本 */
    public static boolean isASCIIText(String text) {
        for (int i = 0, len = text.length(); i < len; i++) {
            if (text.charAt(i) < 32 || text.charAt(i) > 126) {
                return false;
            }
        }
        return true;
    }

    public static void checkNullString(String str) {
        if ("null".equals(str)) {
            return;
        }
        throw new IllegalArgumentException("invalid null str: " + str);
    }

    public static Object parseBool(String str) {
        if (str.equals("true") || str.equals("1")) return true;
        if (str.equals("false") || str.equals("0")) return false;
        throw new IllegalArgumentException("invalid bool str: " + str);
    }

    public static int parseInt(String rawStr) {
        String str = rawStr;
        if (str.indexOf('_') >= 0) {
            str = deleteUnderline(str);
        }
        if (str.isEmpty()) {
            throw new NumberFormatException(rawStr);
        }
        int lookOffset;
        int sign;
        char firstChar = str.charAt(0);
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
        if (str.startsWith("0x", lookOffset) || str.startsWith("0X", lookOffset)) {
            return sign * Integer.parseInt(str, 2, str.length(), 16);
        }
        if (str.startsWith("0b", lookOffset) || str.startsWith("0B", lookOffset)) {
            return sign * Integer.parseInt(str, 3, str.length(), 2);
        }
        return Integer.parseInt(str);
    }

    public static long parseLong(final String rawStr) {
        String str = rawStr;
        if (str.indexOf('_') >= 0) {
            str = deleteUnderline(str);
        }
        if (str.isEmpty()) {
            throw new NumberFormatException(rawStr);
        }
        int lookOffset;
        int sign;
        char firstChar = str.charAt(0);
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
        if (str.startsWith("0x", lookOffset) || str.startsWith("0X", lookOffset)) {
            return sign * Long.parseLong(str, 2, str.length(), 16);
        }
        if (str.startsWith("0b", lookOffset) || str.startsWith("0B", lookOffset)) {
            return sign * Long.parseLong(str, 3, str.length(), 2);
        }
        return Long.parseLong(str);
    }

    public static float parseFloat(String str) {
        if (str.indexOf('_') >= 0) {
            str = deleteUnderline(str);
        }
        return Float.parseFloat(str);
    }

    public static double parseDouble(String str) {
        if (str.indexOf('_') >= 0) {
            str = deleteUnderline(str);
        }
        return Double.parseDouble(str);
    }

    private static String deleteUnderline(String str) {
        int length = str.length();
        if (str.charAt(0) == '-' || str.charAt(length - 1) == '_') {
            throw new NumberFormatException(str);
        }
        StringBuilder sb = new StringBuilder(length - 1);
        boolean hasUnderline = false;
        for (int i = 0; i < length; i++) {
            char c = str.charAt(i);
            if (c == '_') {
                if (hasUnderline) throw new NumberFormatException(str);
                hasUnderline = true;
            } else {
                sb.append(c);
                hasUnderline = false;
            }
        }
        return sb.toString();
    }

    // 正则的性能不好
    private static final Pattern number_rule = Pattern.compile("^([+-]?\\d+\\.\\d+)|([+-]?\\d+)|([+-]?\\.\\d+)$");
    private static final Pattern scientific_rule = Pattern.compile("^[+-]?((\\d+\\.?\\d*)|(\\.\\d+))[Ee][+-]?\\d+$");

    public static boolean isParsable(String str) {
        int length = str.length();
        if (length == 0 || length > 66) { // 最长也不应该比二进制格式长
            return false;
        }
        return NumberUtils.isParsable(str);
    }

    // 修改自commons-codec，避免引入依赖

    private static final char[] DIGITS_UPPER = {
            '0', '1', '2', '3', '4', '5', '6', '7', '8', '9',
            'A', 'B', 'C', 'D', 'E', 'F'};

    static void encodeHex(final byte[] data, final int dataOffset, final int dataLen,
                          final char[] out, final int outOffset) {
        char[] toDigits = DIGITS_UPPER;
        for (int i = dataOffset, j = outOffset; i < dataOffset + dataLen; i++) {
            out[j++] = toDigits[(0xF0 & data[i]) >>> 4];
            out[j++] = toDigits[0x0F & data[i]];
        }
    }

    static byte[] decodeHex(char[] data) {
        byte[] out = new byte[data.length >> 1];
        final int len = data.length;
        if ((len & 0x01) != 0) {
            throw new DsonIOException("Odd number of characters.");
        }

        final int outLen = len >> 1;
        if (out.length < outLen) {
            throw new DsonIOException("Output array is not large enough to accommodate decoded data.");
        }

        // two characters form the hex value.
        for (int i = 0, j = 0; j < len; i++) {
            int f = toDigit(data[j], j) << 4;
            j++;
            f = f | toDigit(data[j], j);
            j++;
            out[i] = (byte) (f & 0xFF);
        }
        return out;
    }

    private static int toDigit(final char ch, final int index) {
        // 相当于Switch-case 字符，返回对应的数字
        final int digit = Character.digit(ch, 16);
        if (digit == -1) {
            throw new DsonIOException("Illegal hexadecimal character " + ch + " at index " + index);
        }
        return digit;
    }

    //

    /**
     * 测试是否是换行符
     * 现在操作系统的换行符只有: \r\n (windows) 和 \n (unix, mac)
     */
    static boolean isCRLF(char c, CharSequence buffer, int pos) {
        if (c == '\n') return true;
        return c == '\r' && (pos + 1 < buffer.length() && buffer.charAt(pos + 1) == '\n');
    }

    /** @return 换行符的长度 */
    static int lengthCRLF(char c) {
        return c == '\r' ? 2 : 1;
    }

}