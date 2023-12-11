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

using System.Text;
using Wjybxx.Dson.IO;

namespace Wjybxx.Dson.Text;

public class DsonPrinter : IDisposable
{
#nullable disable
    private readonly TextWriter _writer;
    private readonly string _lineSeparator;
    private readonly bool _autoClose;

    /** 行缓冲，减少同步写操作 */
    private readonly StringBuilder _builder = new StringBuilder(150);
    private char[] _indentionArray = new char[0];
    private int _indent = 0;

    private string _headLabel;
    private int _column;
#nullable enable

    public DsonPrinter(TextWriter writer, string lineSeparator, bool autoClose) {
        this._writer = writer;
        this._lineSeparator = lineSeparator;
        this._autoClose = autoClose;
    }

    /** 当前列数 */
    public int Column => _column;

    /** 如果当前行尚未打印行首，则返回null */
    public string? HeadLabel => _headLabel;

    /** 当前行是否有内容 */
    public bool HasContent => ContentLength > 0;

    /** 当前行内容的长度 */
    public int ContentLength => _headLabel == null ? _column : _column - _headLabel.Length - 1;

    public TextWriter Writer => _writer;

    #region 普通打印

    /**
     * @apiNote tab增加的列不是固定的...所以其它打印字符串的方法都必须调用该方法，一定程度上降低了性能，不能批量拷贝
     */
    public void Print(char c) {
        _builder.Append(c);
        if (c == '\t') {
            _column--;
            _column += (4 - (_column % 4));
        }
        else {
            _column += 1;
        }
    }

    public void Print(char[] cBuffer) {
        foreach (char c in cBuffer) {
            Print(c);
        }
    }

    public void Print(char[] cBuffer, int offset, int len) {
        BinaryUtils.CheckBuffer(cBuffer.Length, offset, len);
        for (int idx = offset, end = offset + len; idx < end; idx++) {
            Print(cBuffer[idx]);
        }
    }

    public void Print(string text) {
        for (int idx = 0, end = text.Length; idx < end; idx++) {
            Print(text[idx]);
        }
    }

    /**
     * @param start the starting index of the subsequence to be appended.
     * @param end   the end index of the subsequence to be appended.
     */
    public void PrintRange(string text, int start, int end) {
        CheckRange(start, end, text.Length);
        for (int idx = start; idx < end; idx++) {
            Print(text[idx]);
        }
    }

    private static void CheckRange(int start, int end, int len) {
        if (start < 0 || start > end || end > len) {
            throw new IndexOutOfRangeException(
                "start " + start + ", end " + end + ", length " + len);
        }
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
    public void PrintFastPath(char[] cBuffer, int offset, int len) {
        _builder.Append(cBuffer, offset, len);
        _column += len;
    }

    /** @param text 内容中无tab字符 */
    public void PrintFastPath(string text) {
        _builder.Append(text);
        _column += text.Length;
    }

    /** @param text 内容中无tab字符 */
    public void PrintRangeFastPath(string text, int start, int end) {
        _builder.Append(text, start, end);
        _column += (end - start);
    }

    #endregion

    #region dson

    /** 打印行首 */
    public void PrintHead(LineHead lineHead) {
        PrintHead(DsonTexts.GetLabel(lineHead));
    }

    /** 打印行首 */
    public void PrintHead(string label) {
        if (_headLabel != null) {
            throw new InvalidOperationException();
        }
        _builder.Append(label);
        _builder.Append(' ');
        _column += label.Length + 1;
        _headLabel = label;
    }

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
        switch (c) {
            case '\"':
                PrintFastPath("\\\"");
                break;
            case '\\':
                PrintFastPath("\\\\");
                break;
            case '\b':
                PrintFastPath("\\b");
                break;
            case '\f':
                PrintFastPath("\\f");
                break;
            case '\n':
                PrintFastPath("\\n");
                break;
            case '\r':
                PrintFastPath("\\r");
                break;
            case '\t':
                PrintFastPath("\\t");
                break;
            default: {
                if (unicodeChar && (c < 32 || c > 126)) {
                    PrintFastPath("\\u");
                    PrintRangeFastPath((0x10000 + c).ToString("X"), 1, 5);
                }
                else {
                    Print(c);
                }
                break;
            }
        }
    }

    #endregion

    #region 缩进

    /** 换行 */
    public void Println() {
        _builder.Append(_lineSeparator);
        Flush();
        _column = 0;
        _headLabel = null;
    }

    /** 打印缩进 */
    public void PrintIndent() {
        _builder.Append(_indentionArray, 0, _indent);
        _column += _indent;
    }

    /** 打印缩进，可指定一个偏移量 */
    public void PrintIndent(int offset) {
        int len = _indent - offset;
        if (len <= 0) {
            throw new ArgumentException($"invalid offset, indent: {_indent}, offset: {offset}");
        }
        _builder.Append(_indentionArray, offset, len);
        _column += len;
    }

    /** 打印一个空格 */
    public void PrintSpace() {
        _builder.Append(' ');
        _column += 1;
    }

    /** 当前的缩进长度 */
    public int IndentLength() {
        return _indent;
    }

    public void Indent() {
        _indent += 2;
        UpdateIndent();
    }

    public void Retract() {
        if (_indent < 2) {
            throw new InvalidOperationException("indent must be called before retract");
        }
        _indent -= 2;
        UpdateIndent();
    }

    private void UpdateIndent() {
        if (_indent > _indentionArray.Length) {
            _indentionArray = new char[_indent];
            Array.Fill(_indentionArray, ' ');
        }
    }

    #endregion

    #region io

    public void Flush() {
        StringBuilder builder = this._builder;
        if (builder.Length > 0) {
            _writer.Write(this._builder);
            builder.Length = 0;
        }
        _writer.Flush();
    }

    public void Dispose() {
        Flush();
        if (_autoClose) {
            _writer.Dispose();
        }
    }

    #endregion
}