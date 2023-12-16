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

import cn.wjybxx.dson.internal.DsonInternals;

import java.util.List;
import java.util.Objects;

/**
 * @author wjybxx
 * date - 2023/6/2
 */
public class DsonScanner implements AutoCloseable {

    private static final List<DsonTokenType> STRING_TOKEN_TYPES = List.of(DsonTokenType.STRING, DsonTokenType.UNQUOTE_STRING);

    private DsonCharStream charStream;
    private StringBuilder pooledStringBuilder = new StringBuilder(64);
    private final char[] hexBuffer = new char[4];

    public DsonScanner(String dson, DsonMode dsonMode) {
        this.charStream = new StringCharStream(dson, dsonMode);
    }

    public DsonScanner(DsonCharStream charStream) {
        this.charStream = Objects.requireNonNull(charStream);
    }

    @Override
    public void close() {
        if (charStream != null) {
            charStream.close();
            charStream = null;
        }
        if (pooledStringBuilder != null) {
            pooledStringBuilder = null;
        }
    }

    public DsonToken nextToken() {
        if (charStream == null) {
            throw new DsonParseException("Scanner closed");
        }
        int c = skipWhitespace();
        if (c == -1) {
            return new DsonToken(DsonTokenType.EOF, "eof", getPosition());
        }
        return switch (c) {
            case '{' -> {
                // peek下一个字符，判断是否有修饰自身的header
                int nextChar = charStream.read();
                charStream.unread();
                if (nextChar == '@') {
                    yield new DsonToken(DsonTokenType.BEGIN_OBJECT, "{@", getPosition());
                } else {
                    yield new DsonToken(DsonTokenType.BEGIN_OBJECT, "{", getPosition());
                }
            }
            case '[' -> {
                int nextChar = charStream.read();
                charStream.unread();
                if (nextChar == '@') {
                    yield new DsonToken(DsonTokenType.BEGIN_ARRAY, "[@", getPosition());
                } else {
                    yield new DsonToken(DsonTokenType.BEGIN_ARRAY, "[", getPosition());
                }
            }
            case '}' -> new DsonToken(DsonTokenType.END_OBJECT, "}", getPosition());
            case ']' -> new DsonToken(DsonTokenType.END_ARRAY, "]", getPosition());
            case ':' -> new DsonToken(DsonTokenType.COLON, ":", getPosition());
            case ',' -> new DsonToken(DsonTokenType.COMMA, ",", getPosition());
            case '@' -> parseHeaderToken();
            case '"' -> new DsonToken(DsonTokenType.STRING, scanString((char) c), getPosition());
            default -> new DsonToken(DsonTokenType.UNQUOTE_STRING, scanUnquotedString((char) c), getPosition());
        };
    }

    // region common

    private static void checkEof(int c) {
        if (c == -1) {
            throw new DsonParseException("End of file in Dson string.");
        }
    }

    private static void checkToken(List<DsonTokenType> expected, DsonTokenType tokenType, int position) {
        if (!DsonInternals.containsRef(expected, tokenType)) {
            throw invalidTokenType(expected, tokenType, position);
        }
    }

    private static DsonParseException invalidTokenType(List<DsonTokenType> expected, DsonTokenType tokenType, int position) {
        return new DsonParseException(String.format("Invalid Dson Token. Position: %d. Expected: %s. Found: '%s'.",
                position, expected, tokenType));
    }

    private static DsonParseException invalidClassName(String c, int position) {
        return new DsonParseException(String.format("Invalid className. Position: %d. ClassName: '%s'.", position, c));
    }

    private static DsonParseException invalidEscapeSequence(int c, int position) {
        return new DsonParseException(String.format("Invalid escape sequence. Position: %d. Character: '\\%c'.", position, c));
    }

    private static DsonParseException spaceRequired(int position) {
        return new DsonParseException(String.format("Space is required. Position: %d.", position));
    }

    private StringBuilder allocStringBuilder() {
        pooledStringBuilder.setLength(0);
        return pooledStringBuilder;
    }

    private int getPosition() {
        return charStream.getPosition();
    }

    // endregion

    // region header

    private DsonToken parseHeaderToken() {
        try {
            String className = scanClassName();
            if (className.equals("{")) {
                return new DsonToken(DsonTokenType.BEGIN_HEADER, "@{", getPosition());
            }
            return onReadClassName(className);
        } catch (Exception e) {
            throw DsonParseException.wrap(e);
        }
    }

    private String scanClassName() {
        int firstChar = charStream.read();
        if (firstChar < 0) {
            throw invalidClassName("@", getPosition());
        }
        // header是结构体
        if (firstChar == '{') {
            return "{";
        }
        // header是 '@clsName' 简写形式
        String className;
        if (firstChar == '"') {
            className = scanString((char) firstChar);
        } else {
            // 非双引号模式下，只能由安全字符构成
            if (DsonTexts.isUnsafeStringChar(firstChar)) {
                throw invalidClassName(Character.toString((char) firstChar), getPosition());
            }
            // 非双引号模式下，不支持换行继续输入，且clsName后必须是空格或换行符
            DsonCharStream buffer = this.charStream;
            StringBuilder sb = allocStringBuilder();
            sb.append((char) firstChar);
            int c;
            while ((c = buffer.read()) >= 0) {
                if (DsonTexts.isUnsafeStringChar(c)) {
                    break;
                }
                sb.append((char) c);
            }
            if (c == -2) {
                buffer.unread();
            } else if (c != ' ') {
                throw spaceRequired(getPosition());
            }
            className = sb.toString();
        }
        if (DsonInternals.isBlank(className)) {
            throw invalidClassName(className, getPosition());
        }
        return className;
    }

    private DsonToken onReadClassName(String className) {
        final int position = getPosition();
        switch (className) {
            case DsonTexts.LABEL_INT32 -> {
                DsonToken nextToken = nextToken();
                checkToken(STRING_TOKEN_TYPES, nextToken.getType(), position);
                return new DsonToken(DsonTokenType.INT32, DsonTexts.parseInt(nextToken.castAsString()), getPosition());
            }
            case DsonTexts.LABEL_INT64 -> {
                DsonToken nextToken = nextToken();
                checkToken(STRING_TOKEN_TYPES, nextToken.getType(), position);
                return new DsonToken(DsonTokenType.INT64, DsonTexts.parseLong(nextToken.castAsString()), getPosition());
            }
            case DsonTexts.LABEL_FLOAT -> {
                DsonToken nextToken = nextToken();
                checkToken(STRING_TOKEN_TYPES, nextToken.getType(), position);
                return new DsonToken(DsonTokenType.FLOAT, DsonTexts.parseFloat(nextToken.castAsString()), getPosition());
            }
            case DsonTexts.LABEL_DOUBLE -> {
                DsonToken nextToken = nextToken();
                checkToken(STRING_TOKEN_TYPES, nextToken.getType(), position);
                return new DsonToken(DsonTokenType.DOUBLE, DsonTexts.parseDouble(nextToken.castAsString()), getPosition());
            }
            case DsonTexts.LABEL_BOOL -> {
                DsonToken nextToken = nextToken();
                checkToken(STRING_TOKEN_TYPES, nextToken.getType(), position);
                return new DsonToken(DsonTokenType.BOOL, DsonTexts.parseBool(nextToken.castAsString()), getPosition());
            }
            case DsonTexts.LABEL_NULL -> {
                DsonToken nextToken = nextToken();
                checkToken(STRING_TOKEN_TYPES, nextToken.getType(), position);
                DsonTexts.checkNullString(nextToken.castAsString());
                return new DsonToken(DsonTokenType.NULL, null, getPosition());
            }
            case DsonTexts.LABEL_STRING -> {
                DsonToken nextToken = nextToken();
                checkToken(STRING_TOKEN_TYPES, nextToken.getType(), position);
                return new DsonToken(DsonTokenType.STRING, nextToken.castAsString(), getPosition());
            }
            case DsonTexts.LABEL_TEXT -> {
                return new DsonToken(DsonTokenType.STRING, scanText(), getPosition());
            }
        }
        return new DsonToken(DsonTokenType.CLASS_NAME, className, getPosition());
    }

    // endregion

    // region 字符串

    /** @return 如果跳到文件尾则返回 -1 */
    private int skipWhitespace() {
        DsonCharStream buffer = this.charStream;
        int c;
        while ((c = buffer.read()) != -1) {
            if (c == -2) {
                if (buffer.getLineHead() == LineHead.COMMENT) {
                    buffer.skipLine();
                }
                continue;
            }
            if (!DsonTexts.isIndentChar(c)) {
                break;
            }
        }
        return c;
    }

    /**
     * 扫描无引号字符串，无引号字符串不支持切换到独立行
     * （该方法只使用扫描元素，不适合扫描标签）
     *
     * @param firstChar 第一个非空白字符
     */
    private String scanUnquotedString(final char firstChar) {
        DsonCharStream buffer = this.charStream;
        StringBuilder sb = allocStringBuilder();
        sb.append((char) firstChar);
        int c;
        while ((c = buffer.read()) != -1) {
            if (c == -2) {
                if (buffer.getLineHead() == LineHead.COMMENT) {
                    buffer.skipLine();
                }
                continue;
            }
            if (DsonTexts.isUnsafeStringChar(c)) {
                break;
            }
            sb.append((char) c);
        }
        buffer.unread();
        return sb.toString();
    }

    /**
     * 扫描双引号字符串
     *
     * @param quoteChar 引号字符
     */
    private String scanString(char quoteChar) {
        DsonCharStream buffer = this.charStream;
        StringBuilder sb = allocStringBuilder();
        int c;
        while ((c = buffer.read()) != -1) {
            if (c == -2) {
                if (buffer.getLineHead() == LineHead.COMMENT) {
                    buffer.skipLine();
                } else if (buffer.getLineHead() == LineHead.APPEND_LINE) { // 开启新行
                    sb.append('\n');
                } else if (buffer.getLineHead() == LineHead.SWITCH_MODE) { // 进入纯文本模式
                    switch2TextMode(buffer, sb);
                }
            } else if (c == '\\') { // 处理转义字符
                doEscape(buffer, sb, LineHead.APPEND);
            } else if (c == quoteChar) { // 结束
                return sb.toString();
            } else {
                sb.append((char) c);
            }
        }
        throw new DsonParseException("End of file in Dson string.");
    }

    /** 扫描文本段 */
    private String scanText() {
        DsonCharStream buffer = this.charStream;
        StringBuilder sb = allocStringBuilder();
        int c;
        while ((c = buffer.read()) != -1) {
            if (c == -2) {
                if (buffer.getLineHead() == LineHead.COMMENT) {
                    buffer.skipLine();
                } else if (buffer.getLineHead() == LineHead.END_OF_TEXT) { // 读取结束
                    return sb.toString();
                }
                if (buffer.getLineHead() == LineHead.APPEND_LINE) { // 开启新行
                    sb.append('\n');
                } else if (buffer.getLineHead() == LineHead.SWITCH_MODE) { // 进入转义模式
                    switch2EscapeMode(buffer, sb);
                }
            } else {
                sb.append((char) c);
            }
        }
        throw new DsonParseException("End of file in Dson string.");
    }

    private static void switch2TextMode(DsonCharStream buffer, StringBuilder sb) {
        int c;
        while ((c = buffer.read()) != -1) {
            if (c == -2) {
                if (buffer.getLineHead() != LineHead.SWITCH_MODE) { // 退出模式切换
                    buffer.unread();
                    break;
                }
            } else {
                sb.append((char) c);
            }
        }
    }

    private void switch2EscapeMode(DsonCharStream buffer, StringBuilder sb) {
        int c;
        while ((c = buffer.read()) != -1) {
            if (c == -2) {
                if (buffer.getLineHead() != LineHead.SWITCH_MODE) { // 退出模式切换
                    buffer.unread();
                    break;
                }
            } else if (c == '\\') {
                doEscape(buffer, sb, LineHead.SWITCH_MODE);
            } else {
                sb.append((char) c);
            }
        }
    }

    private void doEscape(DsonCharStream buffer, StringBuilder sb, LineHead lockHead) {
        int c = readEscapeChar(buffer, lockHead);
        switch (c) {
            case '"' -> sb.append('"'); // 双引号字符串下，双引号需要转义
            case '\\' -> sb.append('\\');
            case 'b' -> sb.append('\b');
            case 'f' -> sb.append('\f');
            case 'n' -> sb.append('\n');
            case 'r' -> sb.append('\r');
            case 't' -> sb.append('\t');
            case 'u' -> {
                // unicode字符，char是2字节，固定编码为4个16进制数，从高到底
                char[] hexBuffer = this.hexBuffer;
                hexBuffer[0] = (char) readEscapeChar(buffer, lockHead);
                hexBuffer[1] = (char) readEscapeChar(buffer, lockHead);
                hexBuffer[2] = (char) readEscapeChar(buffer, lockHead);
                hexBuffer[3] = (char) readEscapeChar(buffer, lockHead);
                String hex = new String(hexBuffer);
                sb.append((char) Integer.parseInt(hex, 16));
            }
            default -> throw invalidEscapeSequence(c, getPosition());
        }
    }

    /** 读取下一个要转义的字符 -- 只能换行到合并行 */
    private int readEscapeChar(DsonCharStream buffer, LineHead lockHead) {
        int c;
        while (true) {
            c = buffer.read();
            if (c >= 0) {
                return c;
            }
            if (c == -1) {
                throw invalidEscapeSequence('\\', getPosition());
            }
            // c == -2 转义模式下，不可以切换到其它行
            if (buffer.getLineHead() != lockHead) {
                throw invalidEscapeSequence('\\', getPosition());
            }
        }
    }

    // endregion

}