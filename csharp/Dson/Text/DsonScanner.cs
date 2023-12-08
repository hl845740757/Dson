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

using System.Text;

namespace Dson.Text;

public class DsonScanner : IDisposable
{
    private static readonly List<DsonTokenType> STRING_TOKEN_TYPES = DsonInternals.NewList(DsonTokenType.STRING, DsonTokenType.UNQUOTE_STRING);

#nullable disable
    private DsonCharStream _buffer;
    private StringBuilder _pooledStringBuilder = new StringBuilder(64);
    private readonly char[] _hexBuffer = new char[4];
#nullable enable

    public DsonScanner(string dson, DsonMode dsonMode = DsonMode.STANDARD) {
        _buffer = new StringCharStream(dson, dsonMode);
    }

    public DsonScanner(DsonCharStream buffer) {
        _buffer = buffer ?? throw new ArgumentNullException(nameof(buffer));
    }

    public void Dispose() {
        if (_buffer != null) {
            _buffer.Dispose();
            _buffer = null;
        }
        _pooledStringBuilder = null;
    }

    public DsonToken NextToken() {
        if (_buffer == null) {
            throw new DsonParseException("Scanner closed");
        }
        int c = SkipWhitespace();
        if (c == -1) {
            return new DsonToken(DsonTokenType.EOF, "eof", Position);
        }
        switch (c) {
            case '{': {
                // peek下一个字符，判断是否有修饰自身的header
                int nextChar = _buffer.Read();
                _buffer.Unread();
                if (nextChar == '@') {
                    return new DsonToken(DsonTokenType.BEGIN_OBJECT, "{@", Position);
                }
                else {
                    return new DsonToken(DsonTokenType.BEGIN_OBJECT, "{", Position);
                }
            }
            case '[': {
                int nextChar = _buffer.Read();
                _buffer.Unread();
                if (nextChar == '@') {
                    return new DsonToken(DsonTokenType.BEGIN_ARRAY, "[@", Position);
                }
                else {
                    return new DsonToken(DsonTokenType.BEGIN_ARRAY, "[", Position);
                }
            }
            case '}': return new DsonToken(DsonTokenType.END_OBJECT, "}", Position);
            case ']': return new DsonToken(DsonTokenType.END_ARRAY, "]", Position);
            case ':': return new DsonToken(DsonTokenType.COLON, ":", Position);
            case ',': return new DsonToken(DsonTokenType.COMMA, ",", Position);
            case '@': return ParseHeaderToken();
            case '"': return new DsonToken(DsonTokenType.STRING, ScanString((char)c), Position);
            default: return new DsonToken(DsonTokenType.UNQUOTE_STRING, ScanUnquotedString((char)c), Position);
        }
    }

    #region common

    private static void CheckEof(int c) {
        if (c == -1) {
            throw new DsonParseException("End of file in Dson string.");
        }
    }

    private static void CheckToken(List<DsonTokenType> expected, DsonTokenType tokenType, int position) {
        if (!expected.Contains(tokenType)) {
            throw InvalidTokenType(expected, tokenType, position);
        }
    }

    private static DsonParseException InvalidTokenType(List<DsonTokenType> expected, DsonTokenType tokenType, int position) {
        return new DsonParseException($"Invalid Dson Token. Position: {position}. Expected: {expected}. Found: '{tokenType}'.");
    }

    private static DsonParseException InvalidClassName(string c, int position) {
        return new DsonParseException($"Invalid className. Position: {position}. ClassName: '{c}'.");
    }

    private static DsonParseException InvalidEscapeSequence(int c, int position) {
        return new DsonParseException($"Invalid escape sequence. Position: {position}. Character: '\\{c}'.");
    }

    private static DsonParseException SpaceRequired(int position) {
        return new DsonParseException(($"Space is required. Position: {position}."));
    }

    private StringBuilder AllocStringBuilder() {
        _pooledStringBuilder.Length = 0;
        return _pooledStringBuilder;
    }

    private int Position => _buffer.Position;

    #endregion

    #region header

    private DsonToken ParseHeaderToken() {
        try {
            string className = ScanClassName();
            if (className == "{") {
                return new DsonToken(DsonTokenType.BEGIN_HEADER, "@{", Position);
            }
            return OnReadClassName(className);
        }
        catch (Exception e) {
            throw DsonParseException.wrap(e);
        }
    }

    private string ScanClassName() {
        int firstChar = _buffer.Read();
        if (firstChar < 0) {
            throw InvalidClassName("@", Position);
        }
        // header是结构体
        if (firstChar == '{') {
            return "{";
        }
        // header是 '@clsName' 简写形式
        string className;
        if (firstChar == '"') {
            className = ScanString((char)firstChar);
        }
        else {
            // 非双引号模式下，只能由安全字符构成
            if (DsonTexts.isUnsafeStringChar(firstChar)) {
                throw InvalidClassName(char.ToString((char)firstChar), Position);
            }
            // 非双引号模式下，不支持换行继续输入，且clsName后必须是空格或换行符
            DsonCharStream buffer = this._buffer;
            StringBuilder sb = AllocStringBuilder();
            sb.Append((char)firstChar);
            int c;
            while ((c = buffer.Read()) >= 0) {
                if (DsonTexts.isUnsafeStringChar(c)) {
                    break;
                }
                sb.Append((char)c);
            }
            if (c == -2) {
                buffer.Unread();
            }
            else if (c != ' ') {
                throw SpaceRequired(Position);
            }
            className = sb.ToString();
        }
        if (string.IsNullOrWhiteSpace(className)) {
            throw InvalidClassName(className, Position);
        }
        return className;
    }

    private DsonToken OnReadClassName(string className) {
        int position = Position;
        switch (className) {
            case DsonTexts.LABEL_INT32: {
                DsonToken nextToken = NextToken();
                CheckToken(STRING_TOKEN_TYPES, nextToken.Type, position);
                return new DsonToken(DsonTokenType.INT32, DsonTexts.parseInt(nextToken.castAsString()), Position);
            }
            case DsonTexts.LABEL_INT64: {
                DsonToken nextToken = NextToken();
                CheckToken(STRING_TOKEN_TYPES, nextToken.Type, position);
                return new DsonToken(DsonTokenType.INT64, DsonTexts.parseLong(nextToken.castAsString()), Position);
            }
            case DsonTexts.LABEL_FLOAT: {
                DsonToken nextToken = NextToken();
                CheckToken(STRING_TOKEN_TYPES, nextToken.Type, position);
                return new DsonToken(DsonTokenType.FLOAT, DsonTexts.parseFloat(nextToken.castAsString()), Position);
            }
            case DsonTexts.LABEL_DOUBLE: {
                DsonToken nextToken = NextToken();
                CheckToken(STRING_TOKEN_TYPES, nextToken.Type, position);
                return new DsonToken(DsonTokenType.DOUBLE, DsonTexts.parseDouble(nextToken.castAsString()), Position);
            }
            case DsonTexts.LABEL_BOOL: {
                DsonToken nextToken = NextToken();
                CheckToken(STRING_TOKEN_TYPES, nextToken.Type, position);
                return new DsonToken(DsonTokenType.BOOL, DsonTexts.parseBool(nextToken.castAsString()), Position);
            }
            case DsonTexts.LABEL_NULL: {
                DsonToken nextToken = NextToken();
                CheckToken(STRING_TOKEN_TYPES, nextToken.Type, position);
                DsonTexts.checkNullString(nextToken.castAsString());
                return new DsonToken(DsonTokenType.NULL, null, Position);
            }
            case DsonTexts.LABEL_STRING: {
                DsonToken nextToken = NextToken();
                CheckToken(STRING_TOKEN_TYPES, nextToken.Type, position);
                return new DsonToken(DsonTokenType.STRING, nextToken.castAsString(), Position);
            }
            case DsonTexts.LABEL_TEXT: {
                return new DsonToken(DsonTokenType.STRING, ScanText(), Position);
            }
        }
        return new DsonToken(DsonTokenType.CLASS_NAME, className, Position);
    }

    #endregion

    #region 字符串

    /// <summary>
    /// 跳过空白字符
    /// </summary>
    /// <returns>如果跳到文件尾则返回 -1</returns>
    private int SkipWhitespace() {
        DsonCharStream buffer = this._buffer;
        int c;
        while ((c = buffer.Read()) != -1) {
            if (c == -2) {
                if (buffer.LineHead == LineHead.COMMENT) {
                    buffer.SkipLine();
                }
                continue;
            }
            if (!DsonTexts.isIndentChar(c)) {
                break;
            }
        }
        return c;
    }

    /// <summary>
    /// 扫描无引号字符串，无引号字符串不支持切换到独立行
    /// （该方法只使用扫描元素，不适合扫描标签）
    /// </summary>
    /// <param name="firstChar">第一个非空白字符</param>
    /// <returns></returns>
    private string ScanUnquotedString(in char firstChar) {
        DsonCharStream buffer = this._buffer;
        StringBuilder sb = AllocStringBuilder();
        sb.Append((char)firstChar);
        int c;
        while ((c = buffer.Read()) != -1) {
            if (c == -2) {
                if (buffer.LineHead == LineHead.COMMENT) {
                    buffer.SkipLine();
                }
                continue;
            }
            if (DsonTexts.isUnsafeStringChar(c)) {
                break;
            }
            sb.Append((char)c);
        }
        buffer.Unread();
        return sb.ToString();
    }

    /// <summary>
    /// 扫描双引号字符串
    /// </summary>
    /// <param name="quoteChar">引号字符</param>
    /// <returns></returns>
    private string ScanString(char quoteChar) {
        DsonCharStream buffer = this._buffer;
        StringBuilder sb = AllocStringBuilder();
        int c;
        while ((c = buffer.Read()) != -1) {
            if (c == -2) {
                if (buffer.LineHead == LineHead.COMMENT) {
                    buffer.SkipLine();
                }
                else if (buffer.LineHead == LineHead.APPEND_LINE) { // 开启新行
                    sb.Append('\n');
                }
                else if (buffer.LineHead == LineHead.SWITCH_MODE) { // 进入纯文本模式
                    Switch2TextMode(buffer, sb);
                }
            }
            else if (c == '\\') { // 处理转义字符
                DoEscape(buffer, sb, LineHead.APPEND);
            }
            else if (c == quoteChar) { // 结束
                return sb.ToString();
            }
            else {
                sb.Append((char)c);
            }
        }
        throw new DsonParseException("End of file in Dson string.");
    }

    /// <summary>
    /// 扫描纯文本段
    /// </summary>
    /// <returns></returns>
    private string ScanText() {
        DsonCharStream buffer = this._buffer;
        StringBuilder sb = AllocStringBuilder();
        int c;
        while ((c = buffer.Read()) != -1) {
            if (c == -2) {
                if (buffer.LineHead == LineHead.COMMENT) {
                    buffer.SkipLine();
                }
                else if (buffer.LineHead == LineHead.END_OF_TEXT) { // 读取结束
                    return sb.ToString();
                }
                if (buffer.LineHead == LineHead.APPEND_LINE) { // 开启新行
                    sb.Append('\n');
                }
                else if (buffer.LineHead == LineHead.SWITCH_MODE) { // 进入转义模式
                    Switch2EscapeMode(buffer, sb);
                }
            }
            else {
                sb.Append((char)c);
            }
        }
        throw new DsonParseException("End of file in Dson string.");
    }

    private static void Switch2TextMode(DsonCharStream buffer, StringBuilder sb) {
        int c;
        while ((c = buffer.Read()) != -1) {
            if (c == -2) {
                if (buffer.LineHead != LineHead.SWITCH_MODE) { // 退出模式切换
                    buffer.Unread();
                    break;
                }
            }
            else {
                sb.Append((char)c);
            }
        }
    }

    private void Switch2EscapeMode(DsonCharStream buffer, StringBuilder sb) {
        int c;
        while ((c = buffer.Read()) != -1) {
            if (c == -2) {
                if (buffer.LineHead != LineHead.SWITCH_MODE) { // 退出模式切换
                    buffer.Unread();
                    break;
                }
            }
            else if (c == '\\') {
                DoEscape(buffer, sb, LineHead.SWITCH_MODE);
            }
            else {
                sb.Append((char)c);
            }
        }
    }

    private void DoEscape(DsonCharStream buffer, StringBuilder sb, LineHead lockHead) {
        int c = ReadEscapeChar(buffer, lockHead);
        switch (c) {
            case '"':
                sb.Append('"');
                break; // 双引号字符串下，双引号需要转义
            case '\\':
                sb.Append('\\');
                break;
            case 'b':
                sb.Append('\b');
                break;
            case 'f':
                sb.Append('\f');
                break;
            case 'n':
                sb.Append('\n');
                break;
            case 'r':
                sb.Append('\r');
                break;
            case 't':
                sb.Append('\t');
                break;
            case 'u': {
                // unicode字符，char是2字节，固定编码为4个16进制数，从高到底
                char[] hexBuffer = this._hexBuffer;
                hexBuffer[0] = (char)ReadEscapeChar(buffer, lockHead);
                hexBuffer[1] = (char)ReadEscapeChar(buffer, lockHead);
                hexBuffer[2] = (char)ReadEscapeChar(buffer, lockHead);
                hexBuffer[3] = (char)ReadEscapeChar(buffer, lockHead);
                string hex = new string(hexBuffer);
                sb.Append((char)Convert.ToInt32(hex, 16));
                break;
            }
            default: throw InvalidEscapeSequence(c, Position);
        }
    }

    /** 读取下一个要转义的字符 -- 只能换行到合并行 */
    private int ReadEscapeChar(DsonCharStream buffer, LineHead lockHead) {
        int c;
        while (true) {
            c = buffer.Read();
            if (c >= 0) {
                return c;
            }
            if (c == -1) {
                throw InvalidEscapeSequence('\\', Position);
            }
            // c == -2 转义模式下，不可以切换到其它行
            if (buffer.LineHead != lockHead) {
                throw InvalidEscapeSequence('\\', Position);
            }
        }
    }

    #endregion
}