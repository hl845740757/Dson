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

import cn.wjybxx.dson.internal.CollectionUtils;
import org.apache.commons.lang3.StringUtils;

import java.util.List;

/**
 * @author wjybxx
 * date - 2023/6/2
 */
public class DsonScanner implements AutoCloseable {

    private static final List<TokenType> STRING_TOKEN_TYPES = List.of(TokenType.STRING, TokenType.UNQUOTE_STRING);

    private DsonBuffer buffer;
    private StringBuilder sbBuffer = new StringBuilder(256);
    private final char[] hexBuffer = new char[4];

    public DsonScanner(DsonBuffer buffer) {
        this.buffer = buffer;
    }

    @Override
    public void close() {
        if (buffer != null) {
            buffer.close();
            buffer = null;
        }
        if (sbBuffer != null) {
            sbBuffer = null;
        }
    }

    public DsonToken nextToken() {
        if (sbBuffer == null) {
            throw new DsonParseException("Scanner closed");
        }
        int c = skipWhitespace();
        if (c == -1) {
            return new DsonToken(TokenType.EOF, "eof", getPosition());
        }
        return switch (c) {
            case '{' -> {
                // peek下一个字符，判断是否有修饰自身的header
                int nextChar = buffer.read();
                buffer.unread();
                if (nextChar == '@') {
                    yield new DsonToken(TokenType.BEGIN_OBJECT, "{@", getPosition());
                } else {
                    yield new DsonToken(TokenType.BEGIN_OBJECT, "{", getPosition());
                }
            }
            case '[' -> {
                int nextChar = buffer.read();
                buffer.unread();
                if (nextChar == '@') {
                    yield new DsonToken(TokenType.BEGIN_ARRAY, "[@", getPosition());
                } else {
                    yield new DsonToken(TokenType.BEGIN_ARRAY, "[", getPosition());
                }
            }
            case '}' -> new DsonToken(TokenType.END_OBJECT, "}", getPosition());
            case ']' -> new DsonToken(TokenType.END_ARRAY, "]", getPosition());
            case ':' -> new DsonToken(TokenType.COLON, ":", getPosition());
            case ',' -> new DsonToken(TokenType.COMMA, ",", getPosition());
            case '@' -> parseHeaderToken();
            case '"' -> new DsonToken(TokenType.STRING, scanString((char) c), getPosition());
            default -> new DsonToken(TokenType.UNQUOTE_STRING, scanUnquotedString((char) c), getPosition());
        };
    }

    // region common

    /** @return 如果跳到文件尾则返回 -1 */
    private int skipWhitespace() {
        int c;
        while ((c = buffer.read()) != -1 && (c == -2 || Character.isWhitespace(c))) {

        }
        return c;
    }

    private static void checkEof(int c) {
        if (c == -1) {
            throw new DsonParseException("End of file in Dson string.");
        }
    }

    private static void checkToken(List<TokenType> expected, TokenType tokenType, int position) {
        if (!CollectionUtils.containsRef(expected, tokenType)) {
            throw invalidTokenType(expected, tokenType, position);
        }
    }

    private static DsonParseException invalidTokenType(List<TokenType> expected, TokenType tokenType, int position) {
        return new DsonParseException(String.format("Invalid Dson Token. Position: %d. Expected: %s. Found: '%s'.",
                position, expected, tokenType));
    }

    private static DsonParseException invalidInput(int c, int position) {
        return new DsonParseException(String.format("Invalid Dson input. Position: %d. Character: '%c'.", position, c));
    }

    private static DsonParseException invalidClassName(String c, int position) {
        return new DsonParseException(String.format("Invalid className. Position: %d. ClassName: '%s'.", position, c));
    }

    private static DsonParseException invalidEscapeSequence(int c, int position) {
        return new DsonParseException(String.format("Invalid escape sequence. Position: %d. Character: '\\%c'.", position, c));
    }

    private static DsonParseException spaceRequired(int c, int position) {
        return new DsonParseException(String.format("Space is required. Position: %d. Character: '%c'.", position, c));
    }

    private StringBuilder allocStringBuilder() {
        sbBuffer.setLength(0);
        return sbBuffer;
    }

    private int getPosition() {
        return buffer.getPosition();
    }

    private int getPositionAndUnread() {
        int position = buffer.getPosition();
        buffer.unread();
        return position;
    }
    // endregion

    // region header

    private DsonToken parseHeaderToken() {
        try {
            String className = scanClassName();
            return onReadClassName(className);
        } catch (Exception e) {
            throw DsonParseException.wrap(e);
        }
    }

    private String scanClassName() {
        int firstChar = buffer.read();
        checkEof(firstChar);
        // header是结构体
        if (firstChar == '{') {
            return "{";
        }
        // 首字符要么是引号，要是是安全字符
        if (firstChar != '"' && DsonTexts.isUnsafeStringChar(firstChar)) {
            throw invalidInput(firstChar, getPosition());
        }
        String className = firstChar == '"' ? scanString((char) firstChar) : scanUnquotedString((char) firstChar);
        if (StringUtils.isBlank(className)) {
            throw invalidClassName(className, getPosition());
        }
        return className;
    }

    private DsonToken onReadClassName(String className) {
        // className后一定是空格 - 因为scan是扫描到空白字符才停止
        final int position = getPosition();
        switch (className) {
            case DsonTexts.LABEL_INT32 -> {
                DsonToken nextToken = nextToken();
                checkToken(STRING_TOKEN_TYPES, nextToken.getType(), position);
                return new DsonToken(TokenType.INT32, DsonTexts.parseInt(nextToken.castAsString()), getPosition());
            }
            case DsonTexts.LABEL_INT64 -> {
                DsonToken nextToken = nextToken();
                checkToken(STRING_TOKEN_TYPES, nextToken.getType(), position);
                return new DsonToken(TokenType.INT64, DsonTexts.parseLong(nextToken.castAsString()), getPosition());
            }
            case DsonTexts.LABEL_FLOAT -> {
                DsonToken nextToken = nextToken();
                checkToken(STRING_TOKEN_TYPES, nextToken.getType(), position);
                return new DsonToken(TokenType.FLOAT, DsonTexts.parseFloat(nextToken.castAsString()), getPosition());
            }
            case DsonTexts.LABEL_DOUBLE -> {
                DsonToken nextToken = nextToken();
                checkToken(STRING_TOKEN_TYPES, nextToken.getType(), position);
                return new DsonToken(TokenType.DOUBLE, DsonTexts.parseDouble(nextToken.castAsString()), getPosition());
            }
            case DsonTexts.LABEL_BOOL -> {
                DsonToken nextToken = nextToken();
                checkToken(STRING_TOKEN_TYPES, nextToken.getType(), position);
                return new DsonToken(TokenType.BOOL, DsonTexts.parseBool(nextToken.castAsString()), getPosition());
            }
            case DsonTexts.LABEL_NULL -> {
                DsonToken nextToken = nextToken();
                checkToken(STRING_TOKEN_TYPES, nextToken.getType(), position);
                DsonTexts.checkNullString(nextToken.castAsString());
                return new DsonToken(TokenType.NULL, null, getPosition());
            }
            case DsonTexts.LABEL_TEXT -> {
                return new DsonToken(TokenType.STRING, scanText(), getPosition());
            }
        }
        return new DsonToken(TokenType.HEADER, className, getPosition());
    }

    // endregion

    // region 字符串

    /**
     * 扫描无引号字符串，无引号字符串不支持切换到独立行
     *
     * @param firstChar 第一个非空白字符
     */
    private String scanUnquotedString(final char firstChar) {
        DsonBuffer buffer = this.buffer;
        StringBuilder sb = allocStringBuilder();
        sb.append((char) firstChar);
        int c;
        while ((c = buffer.read()) != -1) {
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
        DsonBuffer buffer = this.buffer;
        StringBuilder sb = allocStringBuilder();
        int c;
        while ((c = buffer.readSlowly()) != -1) {
            if (c == -2) {
                if (buffer.lhead() == LheadType.APPEND_LINE) { // 开启新行
                    sb.append('\n');
                } else if (buffer.lhead() == LheadType.SWITCH_MODE) { // 进入纯文本模式
                    switch2TextMode(buffer, sb);
                }
            } else if (c == '\\') { // 处理转义字符
                doEscape(buffer, sb, LheadType.APPEND);
            } else if (c == quoteChar) { // 结束
                return sb.toString();
            } else {
                sb.append((char) c);
            }
        }
        throw new DsonParseException("End of file in Dson string.");
    }

    private static void switch2TextMode(DsonBuffer buffer, StringBuilder sb) {
        int c;
        while ((c = buffer.readSlowly()) != -1) {
            if (c == -2) {
                if (buffer.lhead() != LheadType.SWITCH_MODE) { // 退出模式切换
                    break;
                }
            } else {
                sb.append((char) c);
            }
        }
    }

    private void switch2EscapeMode(DsonBuffer buffer, StringBuilder sb) {
        int c;
        while ((c = buffer.readSlowly()) != -1) {
            if (c == -2) {
                if (buffer.lhead() != LheadType.SWITCH_MODE) { // 退出模式切换
                    break;
                }
            } else if (c == '\\') {
                doEscape(buffer, sb, LheadType.SWITCH_MODE);
            } else {
                sb.append((char) c);
            }
        }
    }

    /** 扫描文本段 */
    private String scanText() {
        // ss的下一行通常是合并行，如果允许换行符代替空格缩进，将与行首规则冲突
        DsonBuffer buffer = this.buffer;
        int indentChar = buffer.readSlowly();
        if (indentChar != ' ') {
            throw spaceRequired(indentChar, getPosition());
        }
        StringBuilder sb = allocStringBuilder();
        int c;
        while ((c = buffer.readSlowly()) != -1) {
            if (c == -2) {
                if (buffer.lhead() == LheadType.END_OF_TEXT) { // 读取结束
                    return sb.toString();
                }
                if (buffer.lhead() == LheadType.APPEND_LINE) { // 开启新行
                    sb.append('\n');
                } else if (buffer.lhead() == LheadType.SWITCH_MODE) { // 进入转义模式
                    switch2EscapeMode(buffer, sb);
                }
            } else {
                sb.append((char) c);
            }
        }
        throw new DsonParseException("End of file in Dson string.");
    }

    private void doEscape(DsonBuffer buffer, StringBuilder sb, LheadType lockLhead) {
        int c = readEscapeChar(buffer, lockLhead);
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
                hexBuffer[0] = (char) readEscapeChar(buffer, lockLhead);
                hexBuffer[1] = (char) readEscapeChar(buffer, lockLhead);
                hexBuffer[2] = (char) readEscapeChar(buffer, lockLhead);
                hexBuffer[3] = (char) readEscapeChar(buffer, lockLhead);
                String hex = new String(hexBuffer);
                sb.append((char) Integer.parseInt(hex, 16));
            }
            default -> throw invalidEscapeSequence(c, getPosition());
        }
    }

    /** 读取下一个要转义的字符 -- 只能换行到合并行 */
    private int readEscapeChar(DsonBuffer buffer, LheadType lockHead) {
        int c;
        while (true) {
            c = buffer.readSlowly();
            if (c >= 0) {
                return c;
            }
            if (c == -1) {
                throw invalidEscapeSequence('\\', getPosition());
            }
            // c == -2 转义模式下，不可以切换到其它行
            if (buffer.lhead() != lockHead) {
                throw invalidEscapeSequence('\\', getPosition());
            }
        }
    }

    // endregion

}