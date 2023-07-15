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

import cn.wjybxx.dson.*;
import cn.wjybxx.dson.io.Chunk;
import cn.wjybxx.dson.types.ObjectRef;
import cn.wjybxx.dson.types.OffsetTimestamp;
import com.google.protobuf.MessageLite;
import org.apache.commons.lang3.StringUtils;

import java.io.Writer;

/**
 * 总指导：
 * 1. token字符尽量不换行，eg：'{'、'['、'@'
 * 2. token字符和内容的空格缩进尽量在行尾
 *
 * @author wjybxx
 * date - 2023/4/21
 */
public class DsonTextWriter extends AbstractDsonWriter {

    private DsonPrinter printer;
    private final DsonTextWriterSettings settings;
    private final StyleOut styleOut = new StyleOut();

    public DsonTextWriter(int recursionLimit, Writer writer, DsonTextWriterSettings settings) {
        super(recursionLimit);
        this.settings = settings;
        this.printer = new DsonPrinter(writer, settings.lineSeparator);
        setContext(new Context().init(null, DsonContextType.TOP_LEVEL, null));
    }

    @Override
    public Context getContext() {
        return (Context) super.getContext();
    }

    @Override
    public Context getPooledContext() {
        return (Context) super.getPooledContext();
    }

    //
    @Override
    public void flush() {
        printer.flush();
    }

    @Override
    public void close() {
        if (printer != null) {
            printer.close();
            printer = null;
        }
        styleOut.reset();
        super.close();
    }

    // region state

    private void writeCurrentName(DsonPrinter printer, DsonType dsonType) {
        Context context = getContext();
        // 打印元素前先检查是否打印了行首
        if (printer.getHeadLabel() == null) {
            printer.printLhead(LheadType.APPEND_LINE);
        }
        // header与外层对象无缩进，且是匿名属性 -- 如果打印多个header，将保持连续
        if (dsonType == DsonType.HEADER) {
            assert context.count == 0;
            context.headerCount++;
            return;
        }
        // 处理value之间分隔符
        if (context.count > 0) {
            printer.printFastPath(",");
        }
        // 先处理长度超出，再处理缩进
        if (printer.getColumn() >= settings.softLineLength) {
            printer.println();
            printer.printLhead(LheadType.APPEND_LINE);
        }
        if (context.style == ObjectStyle.INDENT) {
            if (context.count > 0 && printer.getContentLength() < printer.indentLength()) {
                // 当前字符数小于缩进也不换行，但首个字段需要换行
                printer.printIndent(printer.getContentLength());
            } else if (printer.hasContent()) {
                // 当前行有内容了才换行缩进
                printer.println();
                printer.printLhead(LheadType.APPEND_LINE);
                printer.printIndent();
            }
        } else if (context.count > 0) {
            // 非缩进模式下，元素之间打印一个空格
            printer.print(' ');
        }
        if (context.contextType.isLikeObject()) {
            printStringNoSS(printer, context.curName);
            printer.printFastPath(": ");
        }
        context.count++;
    }

    private void printStringNoSS(DsonPrinter printer, String text) {
        if (canPrintDirectly(text, settings)) {
            printer.printFastPath(text);
        } else {
            printEscaped(text);
        }
    }

    private static boolean canPrintDirectly(String text, DsonTextWriterSettings settings) {
        return DsonTexts.canUnquoteString(text, settings.maxLengthOfUnquoteString)
                && (!settings.unicodeChar || DsonTexts.isASCIIText(text));
    }

    private void printString(DsonPrinter printer, String value, StringStyle style) {
        final DsonTextWriterSettings settings = this.settings;
        switch (style) {
            case AUTO -> {
                if (canPrintDirectly(value, settings)) {
                    printer.printFastPath(value);
                } else if (!settings.enableText || value.length() < settings.softLineLength * settings.lengthFactorOfText) {
                    printEscaped(value);
                } else {
                    printText(value);
                }
            }
            case QUOTE -> printEscaped(value);
            case UNQUOTE -> printer.printFastPath(value);
            case TEXT -> {
                if (settings.enableText) {
                    printText(value);
                } else {
                    printEscaped(value);
                }
            }
        }
    }

    /** 打印双引号String */
    private void printEscaped(String text) {
        boolean unicodeChar = settings.unicodeChar;
        int softLineLength = settings.softLineLength;
        DsonPrinter printer = this.printer;
        printer.print('"');
        for (int i = 0, length = text.length(); i < length; i++) {
            printer.printEscaped(text.charAt(i), unicodeChar);
            if (printer.getColumn() >= softLineLength) {
                printer.println();
                printer.printLhead(LheadType.APPEND);
            }
        }
        printer.print('"');
    }

    /** 纯文本模式打印，要执行换行符 */
    private void printText(String text) {
        int softLineLength = settings.softLineLength;
        DsonPrinter printer = this.printer;
        printer.printFastPath("@ss "); // 开始符
        for (int i = 0, length = text.length(); i < length; i++) {
            char c = text.charAt(i);
            // 要执行文本中的换行符
            if (DsonTexts.isCRLF(c, text, i)) {
                printer.println();
                printer.printLhead(LheadType.APPEND_LINE);
                i += DsonTexts.lengthCRLF(c) - 1; // for循环+1
                continue;
            }
            printer.print(c);
            if (printer.getColumn() > softLineLength) {
                printer.println();
                printer.printLhead(LheadType.APPEND);
            }
        }
        printer.println();
        printer.printLhead(LheadType.END_OF_TEXT); // 结束符
    }

    private void printBinary(byte[] buffer, int offset, int length) {
        int softLineLength = settings.softLineLength;
        DsonPrinter printer = this.printer;
        // 使用小buffer多次编码代替大的buffer，一方面节省内存，一方面控制行长度
        int segment = 8;
        char[] cBuffer = new char[segment * 2];
        int loop = length / segment;
        for (int i = 0; i < loop; i++) {
            DsonTexts.encodeHex(buffer, offset + i * segment, segment, cBuffer, 0);
            printer.printFastPath(cBuffer, 0, cBuffer.length);
            if (printer.getColumn() >= softLineLength) {
                printer.println();
                printer.printLhead(LheadType.APPEND);
            }
        }
        int remain = length - loop * segment;
        if (remain > 0) {
            DsonTexts.encodeHex(buffer, offset + loop * segment, remain, cBuffer, 0);
            printer.printFastPath(cBuffer, 0, remain * 2);
        }
    }

    private void checkLineLength(DsonPrinter printer, LheadType lheadType) {
        if (printer.getColumn() >= settings.softLineLength) {
            printer.println();
            printer.printLhead(lheadType);
        }
    }

    // endregion

    // region 简单值

    @Override
    protected void doWriteInt32(int value, WireType wireType, INumberStyle style) {
        DsonPrinter printer = this.printer;
        writeCurrentName(printer, DsonType.INT32);

        style.toString(value, styleOut.reset());
        if (styleOut.isTyped()) {
            printer.printFastPath("@i ");
        }
        printer.printFastPath(styleOut.getValue());
    }

    @Override
    protected void doWriteInt64(long value, WireType wireType, INumberStyle style) {
        DsonPrinter printer = this.printer;
        writeCurrentName(printer, DsonType.INT64);

        style.toString(value, styleOut.reset());
        if (styleOut.isTyped()) {
            printer.printFastPath("@L ");
        }
        printer.printFastPath(styleOut.getValue());
    }

    @Override
    protected void doWriteFloat(float value, INumberStyle style) {
        DsonPrinter printer = this.printer;
        writeCurrentName(printer, DsonType.FLOAT);

        style.toString(value, styleOut.reset());
        if (styleOut.isTyped()) {
            printer.printFastPath("@f ");
        }
        printer.printFastPath(styleOut.getValue());
    }

    @Override
    protected void doWriteDouble(double value, INumberStyle style) {
        DsonPrinter printer = this.printer;
        writeCurrentName(printer, DsonType.DOUBLE);

        style.toString(value, styleOut.reset());
        if (styleOut.isTyped()) {
            printer.printFastPath("@d ");
        }
        printer.printFastPath(styleOut.getValue());
    }

    @Override
    protected void doWriteBool(boolean value) {
        DsonPrinter printer = this.printer;
        writeCurrentName(printer, DsonType.BOOLEAN);
        printer.printFastPath(value ? "true" : "false");
    }

    @Override
    protected void doWriteString(String value, StringStyle style) {
        DsonPrinter printer = this.printer;
        writeCurrentName(printer, DsonType.STRING);
        printString(printer, value, style);
    }

    @Override
    protected void doWriteNull() {
        DsonPrinter printer = this.printer;
        writeCurrentName(printer, DsonType.NULL);
        printer.printFastPath("null");
    }

    @Override
    protected void doWriteBinary(DsonBinary binary) {
        DsonPrinter printer = this.printer;
        writeCurrentName(printer, DsonType.BINARY);
        printer.printFastPath("{@bin ");
        printer.printFastPath(Integer.toString(binary.getType()));
        printer.printFastPath(", ");
        printBinary(binary.getData(), 0, binary.getData().length);
        printer.print('}');
    }

    @Override
    protected void doWriteBinary(int type, Chunk chunk) {
        DsonPrinter printer = this.printer;
        writeCurrentName(printer, DsonType.BINARY);
        printer.printFastPath("{@bin ");
        printer.printFastPath(Integer.toString(type));
        printer.printFastPath(", ");
        printBinary(chunk.getBuffer(), chunk.getOffset(), chunk.getLength());
        printer.print('}');
    }

    @Override
    protected void doWriteExtInt32(DsonExtInt32 value, WireType wireType, INumberStyle style) {
        DsonPrinter printer = this.printer;
        writeCurrentName(printer, DsonType.EXT_INT32);
        printer.printFastPath("{@ei ");
        printer.printFastPath(Integer.toString(value.getType()));
        printer.printFastPath(", ");
        style.toString(value.getValue(), styleOut.reset());
        printer.printFastPath(styleOut.getValue());
        printer.print('}');
    }

    @Override
    protected void doWriteExtInt64(DsonExtInt64 value, WireType wireType, INumberStyle style) {
        DsonPrinter printer = this.printer;
        writeCurrentName(printer, DsonType.EXT_INT64);
        printer.printFastPath("{@eL ");
        printer.printFastPath(Integer.toString(value.getType()));
        printer.printFastPath(", ");
        style.toString(value.getValue(), styleOut.reset());
        printer.printFastPath(styleOut.getValue());
        printer.print('}');
    }

    @Override
    protected void doWriteExtString(DsonExtString value, StringStyle style) {
        DsonPrinter printer = this.printer;
        writeCurrentName(printer, DsonType.EXT_STRING);
        printer.printFastPath("{@es ");
        printer.printFastPath(Integer.toString(value.getType()));
        printer.printFastPath(", ");
        printString(printer, value.getValue(), style);
        printer.print('}');
    }

    @Override
    protected void doWriteRef(ObjectRef objectRef) {
        DsonPrinter printer = this.printer;
        writeCurrentName(printer, DsonType.REFERENCE);
        if (StringUtils.isBlank(objectRef.getNamespace())
                && objectRef.getType() == 0 && objectRef.getPolicy() == 0) {
            printer.printFastPath("@ref ");
            printStringNoSS(printer, objectRef.getLocalId());
            return;
        }

        printer.printFastPath("{@ref ");
        int count = 0;
        if (objectRef.hasNamespace()) {
            count++;
            printer.printFastPath(ObjectRef.NAMES_NAMESPACE);
            printer.printFastPath(": ");
            printStringNoSS(printer, objectRef.getNamespace());
        }
        if (objectRef.hasLocalId()) {
            if (count++ > 0) printer.printFastPath(", ");
            checkLineLength(printer, LheadType.APPEND_LINE);
            printer.printFastPath(ObjectRef.NAMES_LOCAL_ID);
            printer.printFastPath(": ");
            printStringNoSS(printer, objectRef.getLocalId());
        }
        if (objectRef.getType() != 0) {
            if (count++ > 0) printer.printFastPath(", ");
            checkLineLength(printer, LheadType.APPEND_LINE);
            printer.printFastPath(ObjectRef.NAMES_TYPE);
            printer.printFastPath(": ");
            printer.printFastPath(Integer.toString(objectRef.getType()));
        }
        if (objectRef.getPolicy() != 0) {
            if (count > 0) printer.printFastPath(", ");
            checkLineLength(printer, LheadType.APPEND_LINE);
            printer.printFastPath(ObjectRef.NAMES_POLICY);
            printer.printFastPath(": ");
            printer.printFastPath(Integer.toString(objectRef.getPolicy()));
        }
        printer.print('}');
    }

    @Override
    protected void doWriteTimestamp(OffsetTimestamp timestamp) {
        DsonPrinter printer = this.printer;
        writeCurrentName(printer, DsonType.TIMESTAMP);
        printer.printFastPath("{@dt ");
        if (timestamp.hasDate()) {
            printer.printFastPath(OffsetTimestamp.NAMES_DATE);
            printer.printFastPath(": ");
            printer.printFastPath(OffsetTimestamp.formatDate(timestamp.getSeconds()));
        }
        if (timestamp.hasTime()) {
            if (timestamp.hasDate()) printer.printFastPath(", ");
            checkLineLength(printer, LheadType.APPEND_LINE);
            printer.printFastPath(OffsetTimestamp.NAMES_TIME);
            printer.printFastPath(": ");
            printer.printFastPath(OffsetTimestamp.formatTime(timestamp.getSeconds()));
        }
        if (timestamp.getNanos() > 0) {
            printer.printFastPath(", ");
            checkLineLength(printer, LheadType.APPEND_LINE);
            if (timestamp.canConvertNanosToMillis()) {
                printer.printFastPath(OffsetTimestamp.NAMES_MILLIS);
                printer.printFastPath(": ");
                printer.printFastPath(Integer.toString(timestamp.getMillisOfNanos()));
            } else {
                printer.printFastPath(OffsetTimestamp.NAMES_NANOS);
                printer.printFastPath(": ");
                printer.printFastPath(Integer.toString(timestamp.getNanos()));
            }
        }
        if (timestamp.hasOffset()) {
            printer.printFastPath(", ");
            checkLineLength(printer, LheadType.APPEND_LINE);
            printer.printFastPath(OffsetTimestamp.NAMES_OFFSET);
            printer.printFastPath(": ");
            printer.printFastPath(OffsetTimestamp.formatOffset(timestamp.getOffset()));
        }
        printer.print('}');
    }

    // endregion

    // region 容器

    @Override
    protected void doWriteStartContainer(DsonContextType contextType, DsonType dsonType, ObjectStyle style) {
        DsonPrinter printer = this.printer;
        writeCurrentName(printer, dsonType);

        Context newContext = newContext(getContext(), contextType, dsonType);
        newContext.style = style;

        printer.printFastPath(contextType.startSymbol);
        if (style == ObjectStyle.INDENT) {
            printer.indent(); // 调整缩进
        }

        setContext(newContext);
        this.recursionDepth++;
    }

    @Override
    protected void doWriteEndContainer() {
        Context context = getContext();
        DsonPrinter printer = this.printer;

        if (context.style == ObjectStyle.INDENT) {
            printer.retract(); // 恢复缩进
            // 打印了内容的情况下才换行结束
            if (printer.hasContent() && (context.headerCount > 0 || context.count > 0)) {
                printer.println();
                printer.printLhead(LheadType.APPEND_LINE);
                printer.printIndent();
            }
        }
        printer.printFastPath(context.contextType.endSymbol);

        this.recursionDepth--;
        setContext(context.parent);
        poolContext(context);
    }

    // endregion

    // region 特殊接口

    @Override
    protected void doWriteMessage(int binaryType, MessageLite messageLite) {
        doWriteBinary(new DsonBinary(binaryType, messageLite.toByteArray()));
    }

    @Override
    protected void doWriteValueBytes(DsonType type, byte[] data) {
        throw new UnsupportedOperationException();
    }

    // endregion

    // region context

    private Context newContext(Context parent, DsonContextType contextType, DsonType dsonType) {
        Context context = getPooledContext();
        if (context != null) {
            setPooledContext(null);
        } else {
            context = new Context();
        }
        context.init(parent, contextType, dsonType);
        return context;
    }

    private void poolContext(Context context) {
        context.reset();
        setPooledContext(context);
    }

    protected static class Context extends AbstractDsonWriter.Context {

        ObjectStyle style = ObjectStyle.INDENT;
        int headerCount = 0;
        int count = 0;

        public Context() {
        }

        public void reset() {
            super.reset();
            style = ObjectStyle.INDENT;
            headerCount = 0;
            count = 0;
        }

        @Override
        public Context getParent() {
            return (Context) parent;
        }

    }

    // endregion

}