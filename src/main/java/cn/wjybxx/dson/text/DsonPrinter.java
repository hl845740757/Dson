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

import cn.wjybxx.dson.internal.BinaryUtils;
import org.apache.commons.lang3.exception.ExceptionUtils;

import java.io.Writer;
import java.util.Arrays;
import java.util.Objects;

/**
 * 该接口与{@link DsonScanner}对应
 * Printer和Scanner一样是非结构化的，由外层来实现结构化；
 *
 * @author wjybxx
 * date - 2023/6/5
 */
public final class DsonPrinter implements AutoCloseable {

    private final Writer writer;
    private final String lineSeparator;

    /** 行缓冲，减少同步写操作 */
    private final StringBuilder builder = new StringBuilder(150);
    private char[] indentionArray = new char[0];
    private int indent = 0;

    private String headLabel;
    private int column;

    public DsonPrinter(Writer writer, String lineSeparator) {
        this.writer = Objects.requireNonNull(writer);
        this.lineSeparator = Objects.requireNonNull(lineSeparator);
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
    public boolean hasContent() {
        return getContentLength() > 0;
    }

    /** 当前行内容的长度 */
    public int getContentLength() {
        return headLabel == null ? column : column - headLabel.length() - 1;
    }

    // region 普通打印

    /**
     * @apiNote tab增加的列不是固定的...所以其它打印字符串的方法都必须调用该方法，一定程度上降低了性能，不能批量拷贝
     */
    public void print(char c) {
        try {
            builder.append(c);
            if (c == '\t') {
                column--;
                column += (4 - (column % 4));
            } else {
                column += 1;
            }
        } catch (Exception e) {
            ExceptionUtils.rethrow(e);
        }
    }

    public void print(char[] cBuffer) {
        for (char c : cBuffer) {
            print(c);
        }
    }

    public void print(char[] cBuffer, int offset, int len) {
        BinaryUtils.checkBuffer(cBuffer.length, offset, len);
        for (int idx = offset, end = offset + len; idx < end; idx++) {
            print(cBuffer[idx]);
        }
    }

    public void print(String text) {
        for (int idx = 0, end = text.length(); idx < end; idx++) {
            print(text.charAt(idx));
        }
    }

    /**
     * @param start the starting index of the subsequence to be appended.
     * @param end   the end index of the subsequence to be appended.
     */
    public void printRange(String text, int start, int end) {
        checkRange(start, end, text.length());
        for (int idx = start; idx < end; idx++) {
            print(text.charAt(idx));
        }
    }

    private static void checkRange(int start, int end, int len) {
        if (start < 0 || start > end || end > len) {
            throw new IndexOutOfBoundsException(
                    "start " + start + ", end " + end + ", length " + len);
        }
    }

    public void println(String text) {
        print(text);
        println();
    }

    // endregion

    // region dson

    /** 打印行首 */
    public void printLhead(LheadType lheadType) {
        printLhead(lheadType.label);
    }

    /** 打印行首 */
    public void printLhead(String label) {
        if (headLabel != null) {
            throw new IllegalStateException();
        }
        try {
            builder.append(label);
            builder.append(' ');
            column += label.length() + 1;
            headLabel = label;
        } catch (Exception e) {
            ExceptionUtils.rethrow(e);
        }
    }

    public void printBeginObject() {
        try {
            builder.append('{');
            column += 1;
        } catch (Exception e) {
            ExceptionUtils.rethrow(e);
        }
    }

    public void printEndObject() {
        try {
            builder.append('}');
            column += 1;
        } catch (Exception e) {
            ExceptionUtils.rethrow(e);
        }
    }

    public void printBeginArray() {
        try {
            builder.append('[');
            column += 1;
        } catch (Exception e) {
            ExceptionUtils.rethrow(e);
        }
    }

    public void printEndArray() {
        try {
            builder.append(']');
            column += 1;
        } catch (Exception e) {
            ExceptionUtils.rethrow(e);
        }
    }

    public void printBeginHeader() {
        try {
            builder.append("@{");
            column += 2;
        } catch (Exception e) {
            ExceptionUtils.rethrow(e);
        }
    }

    /** 打印冒号 */
    public void printColon() {
        try {
            builder.append(':');
            column += 1;
        } catch (Exception e) {
            ExceptionUtils.rethrow(e);
        }
    }

    /** 打印逗号 */
    public void printComma() {
        try {
            builder.append(',');
            column += 1;
        } catch (Exception e) {
            ExceptionUtils.rethrow(e);
        }
    }

    /** 打印可能需要转义的字符 */
    public void printEscaped(char c, boolean unicodeChar) {
        switch (c) {
            case '\"' -> print("\\\"");
            case '\\' -> print("\\\\");
            case '\b' -> print("\\b");
            case '\f' -> print("\\f");
            case '\n' -> print("\\n");
            case '\r' -> print("\\r");
            case '\t' -> print("\\t");
            default -> {
                if (unicodeChar && (c < 32 || c > 126)) {
                    print("\\u");
                    printRange(Integer.toHexString(0x10000 + (int) c), 1, 5);
                } else {
                    print(c);
                }
            }
        }
    }

    // endregion

    // region 缩进

    /** 换行 */
    public void println() {
        try {
            builder.append(lineSeparator);
            flush();
            column = 0;
            headLabel = null;
        } catch (Exception e) {
            ExceptionUtils.rethrow(e);
        }
    }

    /** 打印缩进 */
    public void printIndent() {
        try {
            builder.append(indentionArray, 0, indent);
            column += indent;
        } catch (Exception e) {
            ExceptionUtils.rethrow(e);
        }
    }

    /** 打印缩进，可指定一个偏移量 */
    public void printIndent(int offset) {
        int len = indent - offset;
        if (len <= 0) {
            throw new IllegalArgumentException("invalid offset, indent: %d, offset: %d".formatted(indent, offset));
        }
        try {
            builder.append(indentionArray, offset, len);
            column += len;
        } catch (Exception e) {
            ExceptionUtils.rethrow(e);
        }
    }

    /** 打印一个空格 */
    public void printSpace() {
        try {
            builder.append(' ');
            column += 1;
        } catch (Exception e) {
            ExceptionUtils.rethrow(e);
        }
    }

    /** 当前的缩进长度 */
    public int indentLength() {
        return indent;
    }

    public void indent() {
        indent += 2;
        updateIndent();
    }

    public void retract() {
        if (indent < 2) {
            throw new IllegalArgumentException("indent must be called before retract");
        }
        indent -= 2;
        updateIndent();
    }

    private void updateIndent() {
        if (indent > indentionArray.length) {
            indentionArray = new char[indent];
            Arrays.fill(indentionArray, ' ');
        }
    }
    // endregion

    // region io

    public void flush() {
        try {
            StringBuilder builder = this.builder;
            if (builder.length() > 0) {
                // 显式转cBuffer，避免toString的额外开销
                char[] cBuffer = new char[builder.length()];
                builder.getChars(0, cBuffer.length, cBuffer, 0);

                writer.write(cBuffer, 0, cBuffer.length);
                builder.setLength(0);
            }
            writer.flush();
        } catch (Exception e) {
            ExceptionUtils.rethrow(e);
        }
    }

    @Override
    public void close() {
        try {
            flush();
            writer.close();
        } catch (Exception e) {
            ExceptionUtils.rethrow(e);
        }
    }
    // endregion
}