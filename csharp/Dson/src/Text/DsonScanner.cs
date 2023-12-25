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
using System.Collections.Generic;
using System.Text;

#pragma warning disable CS1591
namespace Wjybxx.Dson.Text;

/// <summary>
/// Dson文本扫描器
/// </summary>
public class DsonScanner : IDisposable
{
    private static readonly List<DsonTokenType> StringTokenTypes = DsonInternals.NewList(DsonTokenType.String, DsonTokenType.UnquoteString);

#nullable disable
    private IDsonCharStream _charStream;
    private StringBuilder _pooledStringBuilder = new StringBuilder(64);
    private readonly char[] _hexBuffer = new char[4];
#nullable enable

    public DsonScanner(string dson, DsonMode dsonMode = DsonMode.Standard) {
        _charStream = new StringCharStream(dson, dsonMode);
    }

    public DsonScanner(IDsonCharStream charStream) {
        _charStream = charStream ?? throw new ArgumentNullException(nameof(charStream));
    }

    public void Dispose() {
        if (_charStream != null) {
            _charStream.Dispose();
            _charStream = null;
        }
        _pooledStringBuilder = null;
    }

    /** 扫描下一个Token */
    public DsonToken NextToken() {
        if (_charStream == null) {
            throw new DsonParseException("Scanner closed");
        }
        int c = SkipWhitespace();
        if (c == -1) {
            return new DsonToken(DsonTokenType.Eof, "eof", Position);
        }
        switch (c) {
            case '{': {
                // peek下一个字符，判断是否有修饰自身的header
                int nextChar = _charStream.Read();
                _charStream.Unread();
                if (nextChar == '@') {
                    return new DsonToken(DsonTokenType.BeginObject, "{@", Position);
                }
                else {
                    return new DsonToken(DsonTokenType.BeginObject, "{", Position);
                }
            }
            case '[': {
                int nextChar = _charStream.Read();
                _charStream.Unread();
                if (nextChar == '@') {
                    return new DsonToken(DsonTokenType.BeginArray, "[@", Position);
                }
                else {
                    return new DsonToken(DsonTokenType.BeginArray, "[", Position);
                }
            }
            case '}': return new DsonToken(DsonTokenType.EndObject, "}", Position);
            case ']': return new DsonToken(DsonTokenType.EndArray, "]", Position);
            case ':': return new DsonToken(DsonTokenType.Colon, ":", Position);
            case ',': return new DsonToken(DsonTokenType.Comma, ",", Position);
            case '@': return ParseHeaderToken();
            case '"': return new DsonToken(DsonTokenType.String, ScanString((char)c), Position);
            default: return new DsonToken(DsonTokenType.UnquoteString, ScanUnquotedString((char)c), Position);
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

    private int Position => _charStream.Position;

    #endregion

    #region header

    private DsonToken ParseHeaderToken() {
        try {
            string className = ScanClassName();
            if (className == "{") {
                return new DsonToken(DsonTokenType.BeginHeader, "@{", Position);
            }
            return OnReadClassName(className);
        }
        catch (Exception e) {
            throw DsonParseException.Wrap(e);
        }
    }

    private string ScanClassName() {
        int firstChar = _charStream.Read();
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
            if (DsonTexts.IsUnsafeStringChar(firstChar)) {
                throw InvalidClassName(char.ToString((char)firstChar), Position);
            }
            // 非双引号模式下，不支持换行继续输入，且clsName后必须是空格或换行符
            IDsonCharStream buffer = this._charStream;
            StringBuilder sb = AllocStringBuilder();
            sb.Append((char)firstChar);
            int c;
            while ((c = buffer.Read()) >= 0) {
                if (DsonTexts.IsUnsafeStringChar(c)) {
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
            case DsonTexts.LabelInt32: {
                DsonToken nextToken = NextToken();
                CheckToken(StringTokenTypes, nextToken.Type, position);
                return new DsonToken(DsonTokenType.Int32, DsonTexts.ParseInt(nextToken.CastAsString()), Position);
            }
            case DsonTexts.LabelInt64: {
                DsonToken nextToken = NextToken();
                CheckToken(StringTokenTypes, nextToken.Type, position);
                return new DsonToken(DsonTokenType.Int64, DsonTexts.ParseLong(nextToken.CastAsString()), Position);
            }
            case DsonTexts.LabelFloat: {
                DsonToken nextToken = NextToken();
                CheckToken(StringTokenTypes, nextToken.Type, position);
                return new DsonToken(DsonTokenType.Float, DsonTexts.ParseFloat(nextToken.CastAsString()), Position);
            }
            case DsonTexts.LabelDouble: {
                DsonToken nextToken = NextToken();
                CheckToken(StringTokenTypes, nextToken.Type, position);
                return new DsonToken(DsonTokenType.Double, DsonTexts.ParseDouble(nextToken.CastAsString()), Position);
            }
            case DsonTexts.LabelBool: {
                DsonToken nextToken = NextToken();
                CheckToken(StringTokenTypes, nextToken.Type, position);
                return new DsonToken(DsonTokenType.Bool, DsonTexts.ParseBool(nextToken.CastAsString()), Position);
            }
            case DsonTexts.LabelNull: {
                DsonToken nextToken = NextToken();
                CheckToken(StringTokenTypes, nextToken.Type, position);
                DsonTexts.CheckNullString(nextToken.CastAsString());
                return new DsonToken(DsonTokenType.Null, null, Position);
            }
            case DsonTexts.LabelString: {
                DsonToken nextToken = NextToken();
                CheckToken(StringTokenTypes, nextToken.Type, position);
                return new DsonToken(DsonTokenType.String, nextToken.CastAsString(), Position);
            }
            case DsonTexts.LabelText: {
                return new DsonToken(DsonTokenType.String, ScanText(), Position);
            }
        }
        return new DsonToken(DsonTokenType.ClassName, className, Position);
    }

    #endregion

    #region 字符串

    /// <summary>
    /// 跳过空白字符
    /// </summary>
    /// <returns>如果跳到文件尾则返回 -1</returns>
    private int SkipWhitespace() {
        IDsonCharStream buffer = this._charStream;
        int c;
        while ((c = buffer.Read()) != -1) {
            if (c == -2) {
                if (buffer.LineHead == LineHead.Comment) {
                    buffer.SkipLine();
                }
                continue;
            }
            if (!DsonTexts.IsIndentChar(c)) {
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
        IDsonCharStream buffer = this._charStream;
        StringBuilder sb = AllocStringBuilder();
        sb.Append((char)firstChar);
        int c;
        while ((c = buffer.Read()) != -1) {
            if (c == -2) {
                if (buffer.LineHead == LineHead.Comment) {
                    buffer.SkipLine();
                }
                continue;
            }
            if (DsonTexts.IsUnsafeStringChar(c)) {
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
        IDsonCharStream buffer = this._charStream;
        StringBuilder sb = AllocStringBuilder();
        int c;
        while ((c = buffer.Read()) != -1) {
            if (c == -2) {
                if (buffer.LineHead == LineHead.Comment) {
                    buffer.SkipLine();
                }
                else if (buffer.LineHead == LineHead.AppendLine) { // 开启新行
                    sb.Append('\n');
                }
                else if (buffer.LineHead == LineHead.SwitchMode) { // 进入纯文本模式
                    Switch2TextMode(buffer, sb);
                }
            }
            else if (c == '\\') { // 处理转义字符
                DoEscape(buffer, sb, LineHead.Append);
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
        IDsonCharStream buffer = this._charStream;
        StringBuilder sb = AllocStringBuilder();
        int c;
        while ((c = buffer.Read()) != -1) {
            if (c == -2) {
                if (buffer.LineHead == LineHead.Comment) {
                    buffer.SkipLine();
                }
                else if (buffer.LineHead == LineHead.EndOfText) { // 读取结束
                    return sb.ToString();
                }
                if (buffer.LineHead == LineHead.AppendLine) { // 开启新行
                    sb.Append('\n');
                }
                else if (buffer.LineHead == LineHead.SwitchMode) { // 进入转义模式
                    Switch2EscapeMode(buffer, sb);
                }
            }
            else {
                sb.Append((char)c);
            }
        }
        throw new DsonParseException("End of file in Dson string.");
    }

    private static void Switch2TextMode(IDsonCharStream buffer, StringBuilder sb) {
        int c;
        while ((c = buffer.Read()) != -1) {
            if (c == -2) {
                if (buffer.LineHead != LineHead.SwitchMode) { // 退出模式切换
                    buffer.Unread();
                    break;
                }
            }
            else {
                sb.Append((char)c);
            }
        }
    }

    private void Switch2EscapeMode(IDsonCharStream buffer, StringBuilder sb) {
        int c;
        while ((c = buffer.Read()) != -1) {
            if (c == -2) {
                if (buffer.LineHead != LineHead.SwitchMode) { // 退出模式切换
                    buffer.Unread();
                    break;
                }
            }
            else if (c == '\\') {
                DoEscape(buffer, sb, LineHead.SwitchMode);
            }
            else {
                sb.Append((char)c);
            }
        }
    }

    private void DoEscape(IDsonCharStream buffer, StringBuilder sb, LineHead lockHead) {
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
    private int ReadEscapeChar(IDsonCharStream buffer, LineHead lockHead) {
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