﻿#region LICENSE

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
using System.IO;
using System.Text;
using Wjybxx.Commons.IO;
using Wjybxx.Dson.IO;

#pragma warning disable CS1591
namespace Wjybxx.Dson.Text;

/// <summary>
/// Dson字符打印类
/// </summary>
public class DsonPrinter : IDisposable
{
    /** 默认共享的缩进缓存 -- 4空格 */
    private static readonly char[] SharedIndentionArray = "    ".ToCharArray();

#nullable disable
    private readonly DsonTextWriterSettings _settings;
    private readonly TextWriter _writer;

    /** 行缓冲，减少同步写操作 */
    private StringBuilder _builder;
    /** 是否是Writer内部的builder */
    private bool _backingBuilder;

    /** 缩进字符缓存，减少字符串构建 */
    private char[] _indentionArray = SharedIndentionArray;
    /** 结构体缩进 -- 默认的body缩进 */
    private int _structIndent;

    /** 当前行号 */
    private int _ln;
    /** 当前列号 */
    private int _column;
#nullable enable

    public DsonPrinter(DsonTextWriterSettings settings, TextWriter writer) {
        _settings = settings;
        _writer = writer ?? throw new ArgumentNullException(nameof(writer));
        // 初始化
        if (settings.AccessBackingBuilder && writer is StringWriter stringWriter) {
            _backingBuilder = true;
            _builder = stringWriter.GetStringBuilder();
        } else {
            _builder = settings.StringBuilderPool.Rent();
        }
    }

    #region 属性

    public TextWriter Writer => _writer;

    /** 当前行号 - 初始1 */
    public int Ln => _ln;

    /** 当前列数 - 初始0 */
    public int Column => _column;

    /// <summary>
    /// 格式化输出时body开始列号
    /// </summary>
    public int PrettyBodyColum => _structIndent;

    #endregion

    #region 普通打印

    /**
     * @apiNote tab增加的列不是固定的...所以其它打印字符串的方法都必须调用该方法，一定程度上降低了性能，不能批量拷贝
     */
    public void Print(char c) {
        _builder.Append(c);
        if (c == '\t') {
            _column--;
            _column += (4 - (_column % 4)); // -1 % 4 => -1
        } else {
            _column += 1;
        }
    }

    /** 打印高平面码点 */
    public void PrintHpmCodePoint(char high, char low) {
        _builder.Append(high);
        _builder.Append(low);
        _column += 1;
    }

    public void Print(char[] cBuffer) {
        foreach (char c in cBuffer) {
            Print(c);
        }
    }

    public void Print(char[] cBuffer, int offset, int len) {
        ByteBufferUtil.CheckBuffer(cBuffer.Length, offset, len);
        for (int idx = offset, end = offset + len; idx < end; idx++) {
            Print(cBuffer[idx]);
        }
    }

    public void Print(string text) {
        for (int idx = 0, end = text.Length; idx < end; idx++) {
            Print(text[idx]);
        }
    }

    public void PrintFastPath(char c) {
        _builder.Append(c);
        _column++;
    }

    /** @param cBuffer 内容中无tab字符 */
    public void PrintFastPath(ReadOnlySpan<char> cBuffer) {
        _builder.Append(cBuffer);
        _column += cBuffer.Length;
    }

    /** @param cBuffer 内容中无tab字符 */
    public void PrintFastPath(char[] cBuffer) {
        _builder.Append(cBuffer);
        _column += cBuffer.Length;
    }

    /** @param cBuffer 内容中无tab字符 */
    public void PrintFastPath(char[] cBuffer, int offset, int count) {
        _builder.Append(cBuffer, offset, count);
        _column += count; // c#是count...
    }

    /** @param text 内容中无tab字符 */
    public void PrintFastPath(string text) {
        _builder.Append(text);
        _column += text.Length;
    }

    /** @param text 内容中无tab字符 */
    public void PrintRangeFastPath(string text, int start, int count) {
        _builder.Append(text, start, count);
        _column += count; // c#是count...
    }

    #endregion

    #region dson

    public void PrintBeginObject() {
        _builder.Append('{');
        _column += 1;
    }

    public void PrintEndObject() {
        _builder.Append('}');
        _column += 1;
    }

    public void PrintBeginArray() {
        _builder.Append('[');
        _column += 1;
    }

    public void PrintEndArray() {
        _builder.Append(']');
        _column += 1;
    }

    public void PrintBeginHeader() {
        _builder.Append("@{");
        _column += 2;
    }

    /** 打印冒号 */
    public void PrintColon() {
        _builder.Append(':');
        _column += 1;
    }

    /** 打印逗号 */
    public void PrintComma() {
        _builder.Append(',');
        _column += 1;
    }

    /** 打印可能需要转义的字符 */
    public void PrintEscaped(char c, bool unicodeChar) {
        StringBuilder sb = _builder;
        switch (c) {
            case '\"':
                sb.Append('\\');
                sb.Append('"');
                _column += 2;
                break;
            case '\\':
                sb.Append('\\');
                sb.Append('\\');
                _column += 2;
                break;
            case '\b':
                sb.Append('\\');
                sb.Append('b');
                _column += 2;
                break;
            case '\f':
                sb.Append('\\');
                sb.Append('f');
                _column += 2;
                break;
            case '\n':
                sb.Append('\\');
                sb.Append('n');
                _column += 2;
                break;
            case '\r':
                sb.Append('\\');
                sb.Append('r');
                _column += 2;
                break;
            case '\t':
                sb.Append('\\');
                sb.Append('t');
                _column += 2;
                break;
            default: {
                if (unicodeChar && (c < 32 || c > 126)) {
                    sb.Append('\\');
                    sb.Append('u');
                    sb.Append((0x10000 + c).ToString("X"), 1, 4);
                    _column += 6;
                } else {
                    sb.Append(c);
                    _column += 1;
                }
                break;
            }
        }
    }

    #endregion

    #region 缩进

    /** 换行 */
    public void Println() {
        _builder.Append(_settings.LineSeparator);
        if (_builder.Length >= 4096) {
            Flush(); // 如果每一行都flush，在数量大的情况下会产生大量的io操作，降低性能
        }
        // Flush();
        _ln++;
        _column = 0;
    }

    /** 打印全部缩进 */
    public void PrintIndent() {
        PrintSpaces(_structIndent);
    }

    /** 打印一个空格 */
    public void PrintSpace() {
        _builder.Append(' ');
        _column += 1;
    }

    /** 打印多个空格 -- char可以静默转int，改名安全些 */
    public void PrintSpaces(int count) {
        if (count < 0) throw new ArgumentException(nameof(count));
        if (count == 0) return;
        if (count <= _indentionArray.Length) {
            _builder.Append(_indentionArray, 0, count);
        } else {
            char[] chars = new char[count];
            Array.Fill(chars, ' ');
            _builder.Append(chars);
            // 尝试缓存下来
            if (count - _indentionArray.Length <= 8) {
                _indentionArray = chars;
            }
        }
        _column += count;
    }

    public void Indent() {
        _structIndent += 2;
        UpdateIndent();
    }

    public void Retract() {
        if (_structIndent < 2) {
            throw new InvalidOperationException("indent must be called before retract");
        }
        _structIndent -= 2;
        UpdateIndent();
    }

    private void UpdateIndent() {
        if (_structIndent > _indentionArray.Length) {
            _indentionArray = new char[_structIndent];
            Array.Fill(_indentionArray, ' ');
        }
    }

    #endregion

    #region io

    public void Flush() {
        if (_backingBuilder) {
            return;
        }
        StringBuilder builder = this._builder;
        if (builder.Length > 0) {
            _writer.Write(this._builder);
            builder.Length = 0;
        }
        _writer.Flush();
    }

    public void Dispose() {
        if (_builder == null) {
            return;
        }
        Flush();
        if (!_backingBuilder) {
            _settings.StringBuilderPool.ReturnOne(_builder);
        }
        _builder = null;
        if (_settings.AutoClose) {
            _writer.Dispose();
        }
    }

    #endregion
}