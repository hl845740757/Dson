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
import java.math.BigDecimal;

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
        super.close();
    }

    // region state

    private void writeCurrentName(DsonPrinter printer, DsonType dsonType) {
        Context context = getContext();
        final boolean onlyLhead = isOnlyLhead(printer); // 在打印逗号前记录
        if (context.contextType != DsonContextType.TOP_LEVEL && context.count > 0) {
            printer.print(",");
        }
        // header和外层对象之间不能有空格
        if (dsonType != DsonType.HEADER
                && (context.style == ObjectStyle.INDENT || printer.getColumn() >= settings.softLineLength)) {
            if (printer.getColumn() == 0) {
                // 首行不换行
                printer.printLhead(LheadType.APPEND_LINE);
            } else if (onlyLhead) {
                // 文本结束行不换行 - 少打印一个空格
                printer.printIndent(1);
            } else if (context.style == ObjectStyle.INDENT) {
                // 正常缩进
                printer.println();
                printer.printLhead(LheadType.APPEND_LINE);
                printer.printIndent();
            }
        } else if (context.count > 0) {
            printer.print(' ');
        }
        // header是匿名属性
        if (dsonType != DsonType.HEADER && context.contextType.isLikeObject()) {
            printStringNonSS(printer, context.curName);
            printer.print(": ");
        }

        if (dsonType == DsonType.HEADER) {
            context.headerCount++;
        } else {
            context.count++;
        }
    }

    /**
     * 不能无引号的情况下只回退为双引号模式；通常用于打印短字符串
     */
    private void printStringNonSS(DsonPrinter printer, String text) {
        if (canPrintDirectly(text)) {
            printer.print(text);
        } else {
            printEscaped(text);
        }
    }

    private boolean canPrintDirectly(String text) {
        return DsonTexts.canUnquoteString(text) && (!settings.unicodeChar || DsonTexts.isASCIIText(text));
    }

    private void printString(DsonPrinter printer, String value, StringStyle style) {
        switch (style) {
            case AUTO -> {
                if (canPrintDirectly(value)) {
                    printer.print(value);
                } else if (!settings.enableText || value.length() < settings.softLineLength * settings.lengthFactorOfText) {
                    printEscaped(value);
                } else {
                    printText(value);
                }
            }
            case QUOTE -> printEscaped(value);
            case UNQUOTE -> printer.print(value);
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
            printEscaped(text.charAt(i), printer, unicodeChar);
            if (printer.getColumn() >= softLineLength) {
                printer.println();
                printer.printLhead(LheadType.APPEND);
            }
        }
        printer.print('"');
    }

    private static void printEscaped(char c, DsonPrinter printer, boolean unicodeChar) {
        switch (c) {
            case '\"' -> printer.print("\\\"");
            case '\\' -> printer.print("\\\\");
            case '\b' -> printer.print("\\b");
            case '\f' -> printer.print("\\f");
            case '\n' -> printer.print("\\n");
            case '\r' -> printer.print("\\r");
            case '\t' -> printer.print("\\t");
            default -> {
                if ((c < 32 || c > 126) && unicodeChar) {
                    printer.print("\\u");
                    printer.printSubRange(Integer.toHexString(0x10000 + (int) c), 1, 5);
                    return;
                }
                printer.print(c);
            }
        }
    }

    /** 纯文本模式打印，要执行换行符 */
    private void printText(String text) {
        int softLineLength = settings.softLineLength;
        DsonPrinter printer = this.printer;
        printer.print("@ss "); // 开始符
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
            printer.print(cBuffer);
            if (printer.getColumn() >= softLineLength) {
                printer.println();
                printer.printLhead(LheadType.APPEND);
            }
        }
        int remain = length - loop * segment;
        if (remain > 0) {
            DsonTexts.encodeHex(buffer, offset + loop * segment, remain, cBuffer, 0);
            printer.print(cBuffer, 0, remain * 2);
        }
    }

    private void checkLineLength(DsonPrinter printer, LheadType lheadType) {
        if (printer.getColumn() >= settings.softLineLength) {
            printer.println();
            printer.printLhead(lheadType);
        }
    }

    private static boolean isOnlyLhead(DsonPrinter printer) {
        return printer.getColumn() <= DsonTexts.CONTENT_LHEAD_LENGTH + 1;
    }

    // endregion

    // region 简单值

    @Override
    protected void doWriteInt32(int value, WireType wireType, NumberStyle style) {
        DsonPrinter printer = this.printer;
        writeCurrentName(printer, DsonType.INT32);
        if (style != NumberStyle.SIMPLE) {
            printer.print("@i ");
        }
        switch (style) {
            case BINARY -> printer.print(Integer.toBinaryString(value));
            case HEX -> printer.print(Integer.toHexString(value));
            default -> printer.print(Integer.toString(value));
        }
    }

    @Override
    protected void doWriteInt64(long value, WireType wireType, NumberStyle style) {
        DsonPrinter printer = this.printer;
        writeCurrentName(printer, DsonType.INT64);
        if (style != NumberStyle.SIMPLE) {
            printer.print("@L ");
        }
        switch (style) {
            case BINARY -> printer.print(Long.toBinaryString(value));
            case HEX -> printer.print(Long.toHexString(value));
            default -> printer.print(Long.toString(value));
        }
    }

    @Override
    protected void doWriteFloat(float value, NumberStyle style) {
        DsonPrinter printer = this.printer;
        writeCurrentName(printer, DsonType.FLOAT);
        if (Float.isNaN(value) || Float.isInfinite(value)) { // 特殊值加上标签
            printer.print("@f ");
            printer.print(Float.toString(value));
        } else {
            // 先测试是否是整数，再测试是否禁用科学计数法
            int iv = (int) value;
            if (iv == value) {
                if (style != NumberStyle.SIMPLE) {
                    printer.print("@f ");
                }
                printer.print(Integer.toString(iv));
            } else {
                String str = Float.toString(value);
                if (str.lastIndexOf('E') > 0) {
                    if (settings.disableSci) { // 禁用科学计数法
                        if (style != NumberStyle.SIMPLE) {
                            printer.print("@f ");
                        }
                        printer.print(new BigDecimal(str).stripTrailingZeros().toPlainString());
                    } else {
                        printer.print("@f "); // 科学计数法加标签
                        printer.print(str);
                    }
                } else {
                    if (style != NumberStyle.SIMPLE) {
                        printer.print("@f ");
                    }
                    printer.print(str);
                }
            }
        }
    }

    @Override
    protected void doWriteDouble(double value) {
        DsonPrinter printer = this.printer;
        writeCurrentName(printer, DsonType.DOUBLE);
        if (Double.isNaN(value) || Double.isInfinite(value)) {
            printer.print("@d ");
            printer.print(Double.toString(value));
        } else {
            // 先测试是否是整数，再测试是否禁用科学计数法
            long lv = (long) value;
            if (lv == value) {
                printer.print(Long.toString(lv));
            } else {
                // 总是使用BigDecimal的话性能不好，我们假设出现科学计数法的频率较低，先toString再测试
                String str = Double.toString(value);
                if (str.lastIndexOf('E') > 0) {
                    if (settings.disableSci) {
                        printer.print(new BigDecimal(str).stripTrailingZeros().toPlainString());
                    } else {
                        printer.print("@d "); // 科学计数法加标签
                        printer.print(str);
                    }
                } else {
                    printer.print(str);
                }
            }
        }
    }

    @Override
    protected void doWriteBool(boolean value) {
        DsonPrinter printer = this.printer;
        writeCurrentName(printer, DsonType.BOOLEAN);
        printer.print(value ? "true" : "false");
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
        printer.print("null");
    }

    @Override
    protected void doWriteBinary(DsonBinary binary) {
        DsonPrinter printer = this.printer;
        writeCurrentName(printer, DsonType.BINARY);
        printer.print("{@bin ");
        printer.print(Integer.toString(binary.getType()));
        printer.print(", ");
        printBinary(binary.getData(), 0, binary.getData().length);
        printer.print('}');
    }

    @Override
    protected void doWriteBinary(int type, Chunk chunk) {
        DsonPrinter printer = this.printer;
        writeCurrentName(printer, DsonType.BINARY);
        printer.print("{@bin ");
        printer.print(Integer.toString(type));
        printer.print(", ");
        printBinary(chunk.getBuffer(), chunk.getOffset(), chunk.getLength());
        printer.print('}');
    }

    @Override
    protected void doWriteExtInt32(DsonExtInt32 value, WireType wireType) {
        DsonPrinter printer = this.printer;
        writeCurrentName(printer, DsonType.EXT_INT32);
        printer.print("{@ei ");
        printer.print(Integer.toString(value.getType()));
        printer.print(", ");
        printer.print(Integer.toString(value.getValue()));
        printer.print('}');
    }

    @Override
    protected void doWriteExtInt64(DsonExtInt64 value, WireType wireType) {
        DsonPrinter printer = this.printer;
        writeCurrentName(printer, DsonType.EXT_INT64);
        printer.print("{@eL ");
        printer.print(Integer.toString(value.getType()));
        printer.print(", ");
        printer.print(Long.toString(value.getValue()));
        printer.print('}');
    }

    @Override
    protected void doWriteExtString(DsonExtString value, StringStyle style) {
        DsonPrinter printer = this.printer;
        writeCurrentName(printer, DsonType.EXT_STRING);
        printer.print("{@es ");
        printer.print(Integer.toString(value.getType()));
        printer.print(", ");
        printString(printer, value.getValue(), style);
        printer.print('}');
    }

    @Override
    protected void doWriteRef(ObjectRef objectRef) {
        DsonPrinter printer = this.printer;
        writeCurrentName(printer, DsonType.REFERENCE);
        if (StringUtils.isBlank(objectRef.getNamespace())
                && objectRef.getType() == 0 && objectRef.getPolicy() == 0) {
            printer.print("@ref ");
            printStringNonSS(printer, objectRef.getLocalId());
            return;
        }

        printer.print("{@ref ");
        int count = 0;
        if (objectRef.hasNamespace()) {
            count++;
            printer.print(ObjectRef.NAMES_NAMESPACE);
            printer.print(": ");
            printStringNonSS(printer, objectRef.getNamespace());
        }
        if (objectRef.hasLocalId()) {
            if (count++ > 0) printer.print(", ");
            checkLineLength(printer, LheadType.APPEND_LINE);
            printer.print(ObjectRef.NAMES_LOCAL_ID);
            printer.print(": ");
            printStringNonSS(printer, objectRef.getLocalId());
        }
        if (objectRef.getType() != 0) {
            if (count++ > 0) printer.print(", ");
            checkLineLength(printer, LheadType.APPEND_LINE);
            printer.print(ObjectRef.NAMES_TYPE);
            printer.print(": ");
            printer.print(Integer.toString(objectRef.getType()));
        }
        if (objectRef.getPolicy() != 0) {
            if (count > 0) printer.print(", ");
            checkLineLength(printer, LheadType.APPEND_LINE);
            printer.print(ObjectRef.NAMES_POLICY);
            printer.print(": ");
            printer.print(Integer.toString(objectRef.getPolicy()));
        }
        printer.print('}');
    }

    @Override
    protected void doWriteTimestamp(OffsetTimestamp timestamp) {
        DsonPrinter printer = this.printer;
        writeCurrentName(printer, DsonType.TIMESTAMP);
        printer.print("{@dt ");
        if (timestamp.hasDate()) {
            printer.print(OffsetTimestamp.NAMES_DATE);
            printer.print(": ");
            printer.print(OffsetTimestamp.formatDate(timestamp.getSeconds()));
        }
        if (timestamp.hasTime()) {
            if (timestamp.hasDate()) printer.print(", ");
            checkLineLength(printer, LheadType.APPEND_LINE);
            printer.print(OffsetTimestamp.NAMES_TIME);
            printer.print(": ");
            printer.print(OffsetTimestamp.formatTime(timestamp.getSeconds()));
        }
        if (timestamp.getNanos() > 0) {
            printer.print(", ");
            checkLineLength(printer, LheadType.APPEND_LINE);
            if (timestamp.canConvertNanosToMillis()) {
                printer.print(OffsetTimestamp.NAMES_MILLIS);
                printer.print(": ");
                printer.print(Integer.toString(timestamp.getMillisOfNanos()));
            } else {
                printer.print(OffsetTimestamp.NAMES_NANOS);
                printer.print(": ");
                printer.print(Integer.toString(timestamp.getNanos()));
            }
        }
        if (timestamp.hasOffset()) {
            printer.print(", ");
            checkLineLength(printer, LheadType.APPEND_LINE);
            printer.print(OffsetTimestamp.NAMES_OFFSET);
            printer.print(": ");
            printer.print(OffsetTimestamp.formatOffset(timestamp.getOffset()));
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

        printer.print(contextType.startSymbol);
        if (style == ObjectStyle.INDENT) {
            printer.indent();
        }

        setContext(newContext);
        this.recursionDepth++;
    }

    @Override
    protected void doWriteEndContainer() {
        Context context = getContext();
        DsonPrinter printer = this.printer;

        if (context.style == ObjectStyle.INDENT) {
            printer.retract();
            // 打印了内容的情况下才换行结束
            if (!isOnlyLhead(printer) && (context.headerCount > 0 || context.count > 0)) {
                printer.println();
                printer.printLhead(LheadType.APPEND_LINE);
                printer.printIndent();
            }
        }
        printer.print(context.contextType.endSymbol);

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