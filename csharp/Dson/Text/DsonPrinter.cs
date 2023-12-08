using System.Text;
using Dson.IO;

namespace Dson.Text;

public class DsonPrinter : IDisposable
{
#nullable disable
    private readonly StreamWriter writer;
    private readonly string lineSeparator;
    private readonly bool autoClose;

    /** 行缓冲，减少同步写操作 */
    private readonly StringBuilder builder = new StringBuilder(150);
    private char[] indentionArray = new char[0];
    private int indent = 0;

    private string headLabel;
    private int column;
#nullable enable

    public DsonPrinter(StreamWriter writer, string lineSeparator, bool autoClose) {
        this.writer = writer;
        this.lineSeparator = lineSeparator;
        this.autoClose = autoClose;
    }

    /** 当前列数 */
    public int getColumn() {
        return column;
    }

    /** 如果当前行尚未打印行首，则返回null */
    public String getHeadLabel() {
        return headLabel;
    }

    /** 当前行是否有内容 */
    public bool hasContent() {
        return getContentLength() > 0;
    }

    /** 当前行内容的长度 */
    public int getContentLength() {
        return headLabel == null ? column : column - headLabel.Length - 1;
    }

    public StreamWriter Writer => writer;

    #region 普通打印

    /**
     * @apiNote tab增加的列不是固定的...所以其它打印字符串的方法都必须调用该方法，一定程度上降低了性能，不能批量拷贝
     */
    public void print(char c) {
        builder.Append(c);
        if (c == '\t') {
            column--;
            column += (4 - (column % 4));
        }
        else {
            column += 1;
        }
    }

    public void print(char[] cBuffer) {
        foreach (char c in cBuffer) {
            print(c);
        }
    }

    public void print(char[] cBuffer, int offset, int len) {
        BinaryUtils.CheckBuffer(cBuffer.Length, offset, len);
        for (int idx = offset, end = offset + len; idx < end; idx++) {
            print(cBuffer[idx]);
        }
    }

    public void print(string text) {
        for (int idx = 0, end = text.Length; idx < end; idx++) {
            print(text[idx]);
        }
    }

    /**
     * @param start the starting index of the subsequence to be appended.
     * @param end   the end index of the subsequence to be appended.
     */
    public void printRange(string text, int start, int end) {
        checkRange(start, end, text.Length);
        for (int idx = start; idx < end; idx++) {
            print(text[idx]);
        }
    }

    private static void checkRange(int start, int end, int len) {
        if (start < 0 || start > end || end > len) {
            throw new IndexOutOfRangeException(
                "start " + start + ", end " + end + ", length " + len);
        }
    }

    /** @param cBuffer 内容中无tab字符 */
    public void printFastPath(char[] cBuffer) {
        builder.Append(cBuffer);
        column += cBuffer.Length;
    }

    /** @param cBuffer 内容中无tab字符 */
    public void printFastPath(char[] cBuffer, int offset, int len) {
        builder.Append(cBuffer, offset, len);
        column += len;
    }

    /** @param text 内容中无tab字符 */
    public void printFastPath(string text) {
        builder.Append(text);
        column += text.Length;
    }

    /** @param text 内容中无tab字符 */
    public void printRangeFastPath(string text, int start, int end) {
        builder.Append(text, start, end);
        column += (end - start);
    }

    #endregion

    #region dson

    /** 打印行首 */
    public void printHead(LineHead lineHead) {
        printHead(DsonTexts.GetLabel(lineHead));
    }

    /** 打印行首 */
    public void printHead(String label) {
        if (headLabel != null) {
            throw new InvalidOperationException();
        }
        builder.Append(label);
        builder.Append(' ');
        column += label.Length + 1;
        headLabel = label;
    }

    public void printBeginObject() {
        builder.Append('{');
        column += 1;
    }

    public void printEndObject() {
        builder.Append('}');
        column += 1;
    }

    public void printBeginArray() {
        builder.Append('[');
        column += 1;
    }

    public void printEndArray() {
        builder.Append(']');
        column += 1;
    }

    public void printBeginHeader() {
        builder.Append("@{");
        column += 2;
    }

    /** 打印冒号 */
    public void printColon() {
        builder.Append(':');
        column += 1;
    }

    /** 打印逗号 */
    public void printComma() {
        builder.Append(',');
        column += 1;
    }

    /** 打印可能需要转义的字符 */
    public void printEscaped(char c, bool unicodeChar) {
        switch (c) {
            case '\"':
                printFastPath("\\\"");
                break;
            case '\\':
                printFastPath("\\\\");
                break;
            case '\b':
                printFastPath("\\b");
                break;
            case '\f':
                printFastPath("\\f");
                break;
            case '\n':
                printFastPath("\\n");
                break;
            case '\r':
                printFastPath("\\r");
                break;
            case '\t':
                printFastPath("\\t");
                break;
            default: {
                if (unicodeChar && (c < 32 || c > 126)) {
                    printFastPath("\\u");
                    printRangeFastPath((0x10000 + c).ToString("X"), 1, 5);
                }
                else {
                    print(c);
                }
                break;
            }
        }
    }

    #endregion

    #region 缩进

    /** 换行 */
    public void println() {
        builder.Append(lineSeparator);
        flush();
        column = 0;
        headLabel = null;
    }

    /** 打印缩进 */
    public void printIndent() {
        builder.Append(indentionArray, 0, indent);
        column += indent;
    }

    /** 打印缩进，可指定一个偏移量 */
    public void printIndent(int offset) {
        int len = indent - offset;
        if (len <= 0) {
            throw new ArgumentException($"invalid offset, indent: {indent}, offset: {offset}");
        }
        builder.Append(indentionArray, offset, len);
        column += len;
    }

    /** 打印一个空格 */
    public void printSpace() {
        builder.Append(' ');
        column += 1;
    }

    /** 当前的缩进长度 */
    public int indentLength() {
        return indent;
    }

    public void Indent() {
        indent += 2;
        updateIndent();
    }

    public void retract() {
        if (indent < 2) {
            throw new InvalidOperationException("indent must be called before retract");
        }
        indent -= 2;
        updateIndent();
    }

    private void updateIndent() {
        if (indent > indentionArray.Length) {
            indentionArray = new char[indent];
            Array.Fill(indentionArray, ' ');
        }
    }

    #endregion

    #region io

    public void flush() {
        StringBuilder builder = this.builder;
        if (builder.Length > 0) {
            writer.Write(this.builder);
            builder.Length = 0;
        }
        writer.Flush();
    }

    public void Dispose() {
        flush();
        if (autoClose) {
            writer.Dispose();
        }
    }

    #endregion
}