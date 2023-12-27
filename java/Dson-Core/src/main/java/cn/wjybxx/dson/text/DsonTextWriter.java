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
import cn.wjybxx.dson.internal.CommonsLang3;
import cn.wjybxx.dson.internal.DsonInternals;
import cn.wjybxx.dson.io.DsonChunk;
import cn.wjybxx.dson.types.ObjectRef;
import cn.wjybxx.dson.types.OffsetTimestamp;

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

    private final Writer writer;
    private final DsonTextWriterSettings settings;

    private DsonPrinter printer;
    private final StyleOut styleOut = new StyleOut();

    public DsonTextWriter(DsonTextWriterSettings settings, Writer writer) {
        super(settings);
        this.settings = settings;
        this.writer = writer;
        this.printer = new DsonPrinter(settings, writer);
        setContext(new Context().init(null, DsonContextType.TOP_LEVEL, null));
    }

    public Writer getWriter() {
        return writer;
    }

    @Override
    protected Context getContext() {
        return (Context) super.getContext();
    }

    @Override
    protected Context getPooledContext() {
        return (Context) super.getPooledContext();
    }

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

    //

    // region state

    private void writeCurrentName(DsonPrinter printer, DsonType dsonType) {
        Context context = getContext();
        // 打印元素前先检查是否打印了行首和外部缩进
        if (printer.getColumn() == 0) {
            printLineHead(LineHead.APPEND_LINE);
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
            printLineHead(LineHead.APPEND_LINE);
        }
        if (context.style == ObjectStyle.INDENT) {
            if (context.hasElement() && printer.getColumn() < printer.getPrettyBodyColum()) {
                // 当前行是字符串结束行，字符串结束位置尚未到达缩进，不换行
                printer.printSpace(printer.getPrettyBodyColum() - printer.getColumn());
            } else if (printer.getColumn() > printer.getPrettyBodyColum()) {
                // 当前行有内容了才换行缩进
                printer.println();
                printLineHead(LineHead.APPEND_LINE);
                printer.printBodyIndent();
            }
        } else if (context.hasElement()) {
            // 非缩进模式下，元素之间打印一个空格
            printer.print(' ');
        }
        if (context.contextType.isLikeObject()) {
            printString(printer, context.curName, StringStyle.AUTO_QUOTE);
            printer.printFastPath(": ");
        }
        context.count++;
    }

    private void printString(DsonPrinter printer, String value, StringStyle style) {
        final DsonTextWriterSettings settings = this.settings;
        switch (style) {
            case AUTO -> {
                if (canPrintAsUnquote(value, settings)) {
                    printer.printFastPath(value);
                } else if (canPrintAsText(value, settings)) {
                    printText(value);
                } else {
                    printEscaped(value);
                }
            }
            case AUTO_QUOTE -> {
                if (canPrintAsUnquote(value, settings)) {
                    printer.printFastPath(value);
                } else {
                    printEscaped(value);
                }
            }
            case QUOTE -> {
                printEscaped(value);
            }
            case UNQUOTE -> {
                printer.printFastPath(value);
            }
            case TEXT -> {
                if (settings.enableText) { // 类json模式下一定为false
                    printText(value);
                } else {
                    printEscaped(value);
                }
            }
            default -> throw new AssertionError(style);
        }
    }

    private static boolean canPrintAsUnquote(String str, DsonTextWriterSettings settings) {
        return DsonTexts.canUnquoteString(str, settings.maxLengthOfUnquoteString)
                && (!settings.unicodeChar || DsonTexts.isASCIIText(str));
    }

    private static boolean canPrintAsText(String str, DsonTextWriterSettings settings) {
        return settings.enableText && (str.length() > settings.textStringLength);
    }

    /** 打印双引号String */
    private void printEscaped(String text) {
        boolean unicodeChar = settings.unicodeChar;
        int softLineLength = settings.softLineLength;
        DsonPrinter printer = this.printer;
        int headIndent;
        if (settings.dsonMode == DsonMode.STANDARD) {
            headIndent = settings.stringAlignLeft ? printer.getPrettyBodyColum() : settings.extraIndent;
        } else {
            headIndent = 0; // 字符串不能在非标准模式下缩进
        }
        printer.print('"');
        for (int i = 0, length = text.length(); i < length; i++) {
            printer.printEscaped(text.charAt(i), unicodeChar);
            if (printer.getColumn() >= softLineLength && (i + 1 < length)) {
                printer.println();
                printer.setHeadIndent(headIndent);
                printer.setBodyIndent(0);
                printLineHead(LineHead.APPEND);
            }
        }
        printer.print('"');
    }

    /** 纯文本模式打印，要执行换行符 */
    private void printText(String text) {
        int softLineLength = settings.softLineLength;
        DsonPrinter printer = this.printer;
        int headIndent;
        if (settings.textAlignLeft) {
            headIndent = printer.getPrettyBodyColum();
            printer.printFastPath("@ss"); // 开始符
            printer.println();
            printer.setHeadIndent(headIndent);
            printer.setBodyIndent(0);
            printLineHead(LineHead.APPEND);
        } else {
            headIndent = settings.extraIndent;
            printer.printFastPath("@ss "); // 开始符
        }
        for (int i = 0, length = text.length(); i < length; i++) {
            char c = text.charAt(i);
            // 要执行文本中的换行符
            if (c == '\n') {
                printer.println();
                printer.setHeadIndent(headIndent);
                printer.setBodyIndent(0);
                printLineHead(LineHead.APPEND_LINE);
                continue;
            }
            if (c == '\r' && (i + 1 < length && text.charAt(i + 1) == '\n')) {
                printer.println();
                printer.setHeadIndent(headIndent);
                printer.setBodyIndent(0);
                printLineHead(LineHead.APPEND_LINE);
                i++;
                continue;
            }
            printer.print(c);
            if (printer.getColumn() > softLineLength && (i + 1 < length)) {
                printer.println();
                printer.setHeadIndent(headIndent);
                printer.setBodyIndent(0);
                printLineHead(LineHead.APPEND);
            }
        }
        printer.println();
        printer.setHeadIndent(headIndent);
        printLineHead(LineHead.END_OF_TEXT);  // 结束符
    }

    private void printBinary(byte[] buffer, int offset, int length) {
        DsonPrinter printer = this.printer;
        int softLineLength = this.settings.softLineLength;
        // 使用小buffer多次编码代替大的buffer，一方面节省内存，一方面控制行长度
        int segment = 8;
        char[] cBuffer = new char[segment * 2];
        int loop = length / segment;
        for (int i = 0; i < loop; i++) {
            checkLineLength(printer, softLineLength, LineHead.APPEND_LINE);
            CommonsLang3.encodeHex(buffer, offset + i * segment, segment, cBuffer, 0);
            printer.printFastPath(cBuffer, 0, cBuffer.length);
        }
        int remain = length - loop * segment;
        if (remain > 0) {
            checkLineLength(printer, softLineLength, LineHead.APPEND_LINE);
            CommonsLang3.encodeHex(buffer, offset + loop * segment, remain, cBuffer, 0);
            printer.printFastPath(cBuffer, 0, remain * 2);
        }
    }

    private void checkLineLength(DsonPrinter printer, int softLineLength, LineHead lineHead) {
        if (printer.getColumn() >= softLineLength) {
            printer.println();
            printLineHead(lineHead);
        }
    }

    private void printLineHead(LineHead lineHead) {
        printer.printHeadIndent();
        if (settings.dsonMode == DsonMode.STANDARD) {
            printer.printHead(lineHead);
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
        printer.printFastPath("[@bin ");
        printer.printFastPath(Integer.toString(binary.getType()));
        printer.printFastPath(", ");
        printBinary(binary.getData(), 0, binary.getData().length);
        printer.print(']');
    }

    @Override
    protected void doWriteBinary(int type, DsonChunk chunk) {
        DsonPrinter printer = this.printer;
        writeCurrentName(printer, DsonType.BINARY);
        printer.printFastPath("[@bin ");
        printer.printFastPath(Integer.toString(type));
        printer.printFastPath(", ");
        printBinary(chunk.getBuffer(), chunk.getOffset(), chunk.getLength());
        printer.print(']');
    }

    @Override
    protected void doWriteExtInt32(DsonExtInt32 extInt32, WireType wireType, INumberStyle style) {
        DsonPrinter printer = this.printer;
        writeCurrentName(printer, DsonType.EXT_INT32);
        printer.printFastPath("[@ei ");
        printer.printFastPath(Integer.toString(extInt32.getType()));
        printer.printFastPath(", ");
        if (extInt32.hasValue()) {
            style.toString(extInt32.getValue(), styleOut.reset());
            printer.printFastPath(styleOut.getValue());
        } else {
            printer.printFastPath("null");
        }
        printer.print(']');
    }

    @Override
    protected void doWriteExtInt64(DsonExtInt64 extInt64, WireType wireType, INumberStyle style) {
        DsonPrinter printer = this.printer;
        writeCurrentName(printer, DsonType.EXT_INT64);
        printer.printFastPath("[@eL ");
        printer.printFastPath(Integer.toString(extInt64.getType()));
        printer.printFastPath(", ");
        if (extInt64.hasValue()) {
            style.toString(extInt64.getValue(), styleOut.reset());
            printer.printFastPath(styleOut.getValue());
        } else {
            printer.printFastPath("null");
        }
        printer.print(']');
    }

    @Override
    protected void doWriteExtDouble(DsonExtDouble extDouble, INumberStyle style) {
        DsonPrinter printer = this.printer;
        writeCurrentName(printer, DsonType.EXT_DOUBLE);
        printer.printFastPath("[@ed ");
        printer.printFastPath(Integer.toString(extDouble.getType()));
        printer.printFastPath(", ");
        if (extDouble.hasValue()) {
            style.toString(extDouble.getValue(), styleOut.reset());
            printer.printFastPath(styleOut.getValue());
        } else {
            printer.printFastPath("null");
        }
        printer.print(']');
    }

    @Override
    protected void doWriteExtString(DsonExtString extString, StringStyle style) {
        DsonPrinter printer = this.printer;
        writeCurrentName(printer, DsonType.EXT_STRING);
        printer.printFastPath("[@es ");
        printer.printFastPath(Integer.toString(extString.getType()));
        printer.printFastPath(", ");
        if (extString.hasValue()) {
            printString(printer, extString.getValue(), style);
        } else {
            printer.printFastPath("null");
        }
        printer.print(']');
    }

    @Override
    protected void doWriteRef(ObjectRef objectRef) {
        DsonPrinter printer = this.printer;
        int softLineLength = this.settings.softLineLength;
        writeCurrentName(printer, DsonType.REFERENCE);
        if (DsonInternals.isBlank(objectRef.getNamespace())
                && objectRef.getType() == 0 && objectRef.getPolicy() == 0) {
            printer.printFastPath("@ref "); // 只有localId时简写
            printString(printer, objectRef.getLocalId(), StringStyle.AUTO_QUOTE);
            return;
        }

        printer.printFastPath("{@ref ");
        int count = 0;
        if (objectRef.hasNamespace()) {
            count++;
            printer.printFastPath(ObjectRef.NAMES_NAMESPACE);
            printer.printFastPath(": ");
            printString(printer, objectRef.getNamespace(), StringStyle.AUTO_QUOTE);
        }
        if (objectRef.hasLocalId()) {
            if (count++ > 0) printer.printFastPath(", ");
            checkLineLength(printer, softLineLength, LineHead.APPEND_LINE);
            printer.printFastPath(ObjectRef.NAMES_LOCAL_ID);
            printer.printFastPath(": ");
            printString(printer, objectRef.getLocalId(), StringStyle.AUTO_QUOTE);
        }
        if (objectRef.getType() != 0) {
            if (count++ > 0) printer.printFastPath(", ");
            checkLineLength(printer, softLineLength, LineHead.APPEND_LINE);
            printer.printFastPath(ObjectRef.NAMES_TYPE);
            printer.printFastPath(": ");
            printer.printFastPath(Integer.toString(objectRef.getType()));
        }
        if (objectRef.getPolicy() != 0) {
            if (count > 0) printer.printFastPath(", ");
            checkLineLength(printer, softLineLength, LineHead.APPEND_LINE);
            printer.printFastPath(ObjectRef.NAMES_POLICY);
            printer.printFastPath(": ");
            printer.printFastPath(Integer.toString(objectRef.getPolicy()));
        }
        printer.print('}');
    }

    @Override
    protected void doWriteTimestamp(OffsetTimestamp timestamp) {
        DsonPrinter printer = this.printer;
        int softLineLength = this.settings.softLineLength;
        writeCurrentName(printer, DsonType.TIMESTAMP);
        if (timestamp.getEnables() == OffsetTimestamp.MASK_DATETIME) {
            printer.printFastPath("@dt ");
            printer.printFastPath(OffsetTimestamp.formatDateTime(timestamp.getSeconds()));
            return;
        }

        printer.printFastPath("{@dt ");
        if (timestamp.hasDate()) {
            printer.printFastPath(OffsetTimestamp.NAMES_DATE);
            printer.printFastPath(": ");
            printer.printFastPath(OffsetTimestamp.formatDate(timestamp.getSeconds()));
        }
        if (timestamp.hasTime()) {
            if (timestamp.hasDate()) printer.printFastPath(", ");
            checkLineLength(printer, softLineLength, LineHead.APPEND_LINE);
            printer.printFastPath(OffsetTimestamp.NAMES_TIME);
            printer.printFastPath(": ");
            printer.printFastPath(OffsetTimestamp.formatTime(timestamp.getSeconds()));
        }
        if (timestamp.getNanos() > 0) {
            printer.printFastPath(", ");
            checkLineLength(printer, softLineLength, LineHead.APPEND_LINE);
            if (timestamp.canConvertNanosToMillis()) {
                printer.printFastPath(OffsetTimestamp.NAMES_MILLIS);
                printer.printFastPath(": ");
                printer.printFastPath(Integer.toString(timestamp.convertNanosToMillis()));
            } else {
                printer.printFastPath(OffsetTimestamp.NAMES_NANOS);
                printer.printFastPath(": ");
                printer.printFastPath(Integer.toString(timestamp.getNanos()));
            }
        }
        if (timestamp.hasOffset()) {
            printer.printFastPath(", ");
            checkLineLength(printer, softLineLength, LineHead.APPEND_LINE);
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
            if (context.hasElement() && printer.getColumn() > printer.getPrettyBodyColum()) {
                printer.println();
                printLineHead(LineHead.APPEND_LINE);
                printer.printBodyIndent();
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
    public void writeSimpleHeader(String clsName) {
        Context context = getContext();
        if (!canPrintAsUnquote(clsName, settings)) {
            super.writeSimpleHeader(clsName);
            return;
        }
        if (context.contextType == DsonContextType.OBJECT && context.state == DsonWriterState.NAME) {
            context.setState(DsonWriterState.VALUE);
        }
        autoStartTopLevel(context);
        ensureValueState(context);

        DsonPrinter printer = this.printer;
        writeCurrentName(printer, DsonType.HEADER);
        // header总是使用 @{} 包起来，提高辨识度
        printer.print("@{");
        printer.printFastPath(clsName);
        printer.print('}');
        setNextState();
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

        boolean hasElement() {
            return headerCount > 0 || count > 0;
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