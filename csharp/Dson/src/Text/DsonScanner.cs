#region LICENSE

//  Copyright 2023-2024 wjybxx(845740757@qq.com)
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
using System.Diagnostics;
using System.Text;
using Wjybxx.Commons.IO;
using Wjybxx.Commons.Pool;
using Wjybxx.Dson.Internal;
using Wjybxx.Dson.IO;

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
    private IObjectPool<StringBuilder> _builderPool;
    private StringBuilder _pooledStringBuilder;
    private readonly char[] _hexBuffer = new char[4];
#nullable enable

    public DsonScanner(string dson, IObjectPool<StringBuilder>? builderPool = null)
        : this(IDsonCharStream.NewCharStream(dson), builderPool) {
    }

    public DsonScanner(IDsonCharStream charStream, IObjectPool<StringBuilder>? builderPool = null) {
        _charStream = charStream ?? throw new ArgumentNullException(nameof(charStream));
        _builderPool = builderPool ?? LocalStringBuilderPool.Instance;
        _pooledStringBuilder = _builderPool.Rent();
    }

    public void Dispose() {
        if (_charStream != null) {
            _charStream.Dispose();
            _charStream = null;
        }
        if (_builderPool != null) {
            _builderPool.ReturnOne(_pooledStringBuilder);
            _builderPool = null;
            _pooledStringBuilder = null;
        }
    }

    /// <summary>
    /// 扫描下一个token
    /// </summary>
    /// <param name="skipValue">是否跳过值的解析；如果为true，则仅扫描而不截取内容解析；这对于快速扫描确定位置时特别有用</param>
    /// <returns></returns>
    /// <exception cref="DsonParseException"></exception>
    public DsonToken NextToken(bool skipValue = false) {
        if (_charStream == null) {
            throw new DsonParseException("Scanner closed");
        }
        while (true) {
            int c = SkipWhitespace();
            if (c == -1) {
                return new DsonToken(DsonTokenType.Eof, "eof", Position);
            }
            switch (c) {
                case '{': return new DsonToken(DsonTokenType.BeginObject, "{", Position);
                case '[': return new DsonToken(DsonTokenType.BeginArray, "[", Position);
                case '}': return new DsonToken(DsonTokenType.EndObject, "}", Position);
                case ']': return new DsonToken(DsonTokenType.EndArray, "]", Position);
                case ':': return new DsonToken(DsonTokenType.Colon, ":", Position);
                case ',': return new DsonToken(DsonTokenType.Comma, ",", Position);
                case '"': return new DsonToken(DsonTokenType.String, ScanString((char)c, skipValue), Position);
                case '@': return ParseTypeToken(skipValue);
                case '/': {
                    SkipComment();
                    continue;
                }
                default: return new DsonToken(DsonTokenType.UnquoteString, ScanUnquotedString((char)c, skipValue), Position);
            }
        }
    }

    #region common

    private static void EnsureStringToken(DsonTokenType tokenType, int position) {
        if (tokenType != DsonTokenType.UnquoteString && tokenType != DsonTokenType.String) {
            throw InvalidTokenType(StringTokenTypes, tokenType, position);
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

    private DsonToken ParseTypeToken(bool skipValue) {
        int firstChar = _charStream.Read();
        if (firstChar < 0) {
            throw InvalidClassName("@", Position);
        }
        // '@{' 对应的是header，header可能是 {k:v} 或 @{clsName} 简写形式 -- 需要判别
        if (firstChar == '{') {
            return ScanHeader();
        }
        // '@' 对应的是内建值类型，@i @L ...
        return ScanBuiltinValue(firstChar, skipValue);
    }

    /** header不处理跳过逻辑 -- 1.header信息很重要 2.header比例较低 */
    private DsonToken ScanHeader() {
        IDsonCharStream buffer = this._charStream;
        int beginPos = buffer.Position;
        int firstChar = SkipWhitespace(); // {}下跳过空白字符
        if (firstChar < 0) {
            throw InvalidClassName("@{", Position);
        }
        string className;
        if (firstChar == '"') {
            className = ScanString((char)firstChar, false)!;
        } else {
            // 非双引号模式下，只能由安全字符构成
            if (DsonTexts.IsUnsafeStringChar(firstChar)) {
                throw InvalidClassName(char.ToString((char)firstChar), Position);
            }
            StringBuilder sb = AllocStringBuilder();
            sb.Append((char)firstChar);
            int c;
            while ((c = buffer.Read()) >= 0) {
                if (DsonTexts.IsUnsafeStringChar(c)) {
                    break;
                }
                sb.Append((char)c);
            }
            if (c < 0 || DsonTexts.IsUnsafeStringChar(c)) {
                buffer.Unread();
            }
            className = sb.ToString();
        }
        // {} 模式下，下一个字符必须是 ':' 或 '}‘
        int nextChar = SkipWhitespace();
        if (nextChar == ':') { // 结构体样式，需要回退
            while (buffer.Position > beginPos) {
                buffer.Unread();
            }
            return new DsonToken(DsonTokenType.BeginHeader, "{", beginPos);
        } else if (nextChar == '}') { // 简单缩写形式
            return new DsonToken(DsonTokenType.SimpleHeader, className, Position);
        } else {
            throw InvalidClassName(className, Position);
        }
    }

    /** 内建值无引号，且类型标签后必须是空格或换行缩进 */
    private DsonToken ScanBuiltinValue(int firstChar, bool skipValue) {
        Debug.Assert(firstChar != '"');
        // 非双引号模式下，只能由安全字符构成
        if (DsonTexts.IsUnsafeStringChar(firstChar)) {
            throw InvalidClassName(char.ToString((char)firstChar), Position);
        }
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
        // // 无{}模式下，clsName后必须是空格或换行缩进
        if (c == -2) {
            buffer.Unread();
        } else if (c != ' ') {
            throw SpaceRequired(Position);
        }
        string className = sb.ToString();
        if (string.IsNullOrWhiteSpace(className)) {
            throw InvalidClassName(className, Position);
        }
        return OnReadClassName(className, skipValue);
    }

    private DsonToken OnReadClassName(string className, bool skipValue) {
        int position = Position;
        switch (className) {
            case DsonTexts.LabelInt32: {
                DsonToken nextToken = NextToken(skipValue);
                EnsureStringToken(nextToken.Type, position);
                if (skipValue) {
                    return new DsonToken(DsonTokenType.Int32, null, Position);
                }
                return new DsonToken(DsonTokenType.Int32, DsonTexts.ParseInt(nextToken.CastAsString()), Position);
            }
            case DsonTexts.LabelInt64: {
                DsonToken nextToken = NextToken(skipValue);
                EnsureStringToken(nextToken.Type, position);
                if (skipValue) {
                    return new DsonToken(DsonTokenType.Int64, null, Position);
                }
                return new DsonToken(DsonTokenType.Int64, DsonTexts.ParseLong(nextToken.CastAsString()), Position);
            }
            case DsonTexts.LabelFloat: {
                DsonToken nextToken = NextToken(skipValue);
                EnsureStringToken(nextToken.Type, position);
                if (skipValue) {
                    return new DsonToken(DsonTokenType.Float, null, Position);
                }
                return new DsonToken(DsonTokenType.Float, DsonTexts.ParseFloat(nextToken.CastAsString()), Position);
            }
            case DsonTexts.LabelDouble: {
                DsonToken nextToken = NextToken(skipValue);
                EnsureStringToken(nextToken.Type, position);
                if (skipValue) {
                    return new DsonToken(DsonTokenType.Double, null, Position);
                }
                return new DsonToken(DsonTokenType.Double, DsonTexts.ParseDouble(nextToken.CastAsString()), Position);
            }
            case DsonTexts.LabelBool: {
                DsonToken nextToken = NextToken(skipValue);
                EnsureStringToken(nextToken.Type, position);
                if (skipValue) {
                    return new DsonToken(DsonTokenType.Bool, null, Position);
                }
                return new DsonToken(DsonTokenType.Bool, DsonTexts.ParseBool(nextToken.CastAsString()), Position);
            }
            case DsonTexts.LabelNull: {
                DsonToken nextToken = NextToken(skipValue);
                EnsureStringToken(nextToken.Type, position);
                if (skipValue) {
                    return new DsonToken(DsonTokenType.Null, null, Position);
                }
                DsonTexts.CheckNullString(nextToken.CastAsString());
                return new DsonToken(DsonTokenType.Null, null, Position);
            }
            case DsonTexts.LabelString: {
                DsonToken nextToken = NextToken(skipValue);
                EnsureStringToken(nextToken.Type, position);
                return new DsonToken(DsonTokenType.String, nextToken.CastAsString(), Position);
            }
            case DsonTexts.LabelText: {
                return new DsonToken(DsonTokenType.String, ScanText(skipValue), Position);
            }
            case DsonTexts.LabelStringLine: {
                return new DsonToken(DsonTokenType.String, ScanSingleLineText(skipValue), Position);
            }
        }
        return new DsonToken(DsonTokenType.BuiltinStruct, className, Position);
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

    private void SkipComment() {
        IDsonCharStream buffer = this._charStream;
        int nextChar = buffer.Read();
        if (nextChar != '/') {
            throw new DsonParseException("invalid comment format: Single slash, position: " + Position);
        }
        buffer.SkipLine();
    }

    /// <summary>
    /// 扫描无引号字符串，无引号字符串不支持切换到独立行
    /// （该方法只使用扫描元素，不适合扫描标签）
    /// </summary>
    /// <param name="firstChar">第一个非空白字符</param>
    /// <param name="skipValue">是否跳过结果</param>
    /// <returns></returns>
    private string? ScanUnquotedString(char firstChar, bool skipValue) {
        if (skipValue) {
            SkipUnquotedString();
            return null;
        }
        StringBuilder sb = AllocStringBuilder();
        ScanUnquotedString(firstChar, sb);
        return sb.ToString();
    }

    /** 无引号字符串应该的占比是极高的，skip值得处理 */
    private void SkipUnquotedString() {
        IDsonCharStream buffer = this._charStream;
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
        }
        buffer.Unread();
    }

    private void ScanUnquotedString(char firstChar, StringBuilder sb) {
        IDsonCharStream buffer = this._charStream;
        sb.Append(firstChar);
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
    }

    /// <summary>
    /// 扫描双引号字符串
    /// </summary>
    /// <param name="quoteChar">引号字符</param>
    /// <param name="skipValue">是否跳过结果</param>
    /// <returns></returns>
    private string? ScanString(char quoteChar, bool skipValue) {
        StringBuilder sb = AllocStringBuilder();
        ScanString(quoteChar, sb);
        return skipValue ? null : sb.ToString();
    }

    private void ScanString(char quoteChar, StringBuilder sb) {
        IDsonCharStream buffer = this._charStream;
        int c;
        while ((c = buffer.Read()) != -1) {
            if (c == -2) {
                if (buffer.LineHead == LineHead.Comment) {
                    buffer.SkipLine();
                } else if (buffer.LineHead == LineHead.AppendLine) { // 开启新行
                    sb.Append('\n');
                } else if (buffer.LineHead == LineHead.SwitchMode) { // 进入纯文本模式
                    Switch2TextMode(buffer, sb);
                }
            } else if (c == '\\') { // 处理转义字符
                DoEscape(buffer, sb, LineHead.Append);
            } else if (c == quoteChar) { // 结束
                return;
            } else {
                sb.Append((char)c);
            }
        }
        throw new DsonParseException("End of file in Dson string.");
    }

    /// <summary>
    /// 扫描纯文本段
    /// </summary>
    /// <param name="skipValue">是否跳过结果</param>
    /// <returns></returns>
    private string? ScanText(bool skipValue) {
        if (skipValue) {
            SkipText();
            return null;
        }
        StringBuilder sb = AllocStringBuilder();
        ScanText(sb);
        return sb.ToString();
    }

    private void SkipText() {
        IDsonCharStream buffer = this._charStream;
        int c;
        while ((c = buffer.Read()) != -1) {
            if (c == -2 && buffer.LineHead == LineHead.EndOfText) {
                break;
            }
        }
        throw new DsonParseException("End of file in Dson string.");
    }

    private void ScanText(StringBuilder sb) {
        IDsonCharStream buffer = this._charStream;
        int c;
        while ((c = buffer.Read()) != -1) {
            if (c == -2) {
                if (buffer.LineHead == LineHead.Comment) {
                    buffer.SkipLine();
                } else if (buffer.LineHead == LineHead.EndOfText) { // 读取结束
                    return;
                }
                if (buffer.LineHead == LineHead.AppendLine) { // 开启新行
                    sb.Append('\n');
                } else if (buffer.LineHead == LineHead.SwitchMode) { // 进入转义模式
                    Switch2EscapeMode(buffer, sb);
                }
            } else {
                sb.Append((char)c);
            }
        }
        throw new DsonParseException("End of file in Dson string.");
    }

    /** 扫描单行文本 */
    private string? ScanSingleLineText(bool skipValue) {
        if (skipValue) {
            _charStream.SkipLine();
            return null;
        }
        StringBuilder sb = AllocStringBuilder();
        ScanSingleLineText(sb);
        return sb.ToString();
    }

    private void ScanSingleLineText(StringBuilder sb) {
        IDsonCharStream buffer = this._charStream;
        int c;
        while ((c = buffer.Read()) >= 0) {
            sb.Append((char)c);
        }
        buffer.Unread();
    }

    private static void Switch2TextMode(IDsonCharStream buffer, StringBuilder sb) {
        int c;
        while ((c = buffer.Read()) != -1) {
            if (c == -2) {
                if (buffer.LineHead != LineHead.SwitchMode) { // 退出模式切换
                    buffer.Unread();
                    break;
                }
            } else {
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
            } else if (c == '\\') {
                DoEscape(buffer, sb, LineHead.SwitchMode);
            } else {
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