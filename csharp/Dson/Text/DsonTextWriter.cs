using System.Diagnostics;
using Dson.IO;
using Dson.Types;
using Google.Protobuf.WellKnownTypes;

namespace Dson.Text;

public class DsonTextWriter : AbstractDsonWriter<string>
{
    private readonly StreamWriter writer;
    private readonly DsonTextWriterSettings settings;

    private DsonPrinter printer;

    public DsonTextWriter(DsonTextWriterSettings settings, StreamWriter writer)
        : base(settings) {
        this.settings = settings;
        this.writer = writer;
        this.printer = new DsonPrinter(writer, settings.lineSeparator, settings.autoClose);
        SetContext(new Context().init(null, DsonContextType.TOP_LEVEL, DsonTypes.INVALID));
    }

    public StreamWriter StreamWriter => writer ?? throw new ObjectDisposedException("writer");

    protected override Context GetContext() {
        return (Context)_context;
    }

    protected override Context? GetPooledContext() {
        return (Context)base.GetPooledContext();
    }

    public override void Flush() {
        printer?.flush();
    }

    public override void Dispose() {
        if (Settings.autoClose) {
            printer?.Dispose();
            printer = null!;
        }
        base.Dispose();
    }

    #region state

    private void writeCurrentName(DsonPrinter printer, DsonType dsonType) {
        Context context = GetContext();
        // 打印元素前先检查是否打印了行首
        if (printer.getHeadLabel() == null) {
            printLineHead(LineHead.APPEND_LINE);
        }
        // header与外层对象无缩进，且是匿名属性 -- 如果打印多个header，将保持连续
        if (dsonType == DsonType.HEADER) {
            Debug.Assert(context.count == 0);
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
        if (context.style == ObjectStyle.Indent) {
            if (context.count > 0 && printer.getContentLength() < printer.indentLength()) {
                // 当前字符数小于缩进也不换行，但首个字段需要换行
                printer.printIndent(printer.getContentLength());
            }
            else if (printer.hasContent()) {
                // 当前行有内容了才换行缩进
                printer.println();
                printLineHead(LineHead.APPEND_LINE);
                printer.printIndent();
            }
        }
        else if (context.count > 0) {
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
        DsonTextWriterSettings settings = this.settings;
        switch (style) {
            case StringStyle.Auto: {
                if (canPrintAsUnquote(value, settings)) {
                    printer.printFastPath(value);
                }
                else if (canPrintAsText(value, settings)) {
                    printText(value);
                }
                else {
                    printEscaped(value);
                }
                break;
            }
            case StringStyle.AUTO_QUOTE: {
                if (canPrintAsUnquote(value, settings)) {
                    printer.printFastPath(value);
                }
                else {
                    printEscaped(value);
                }
                break;
            }
            case StringStyle.QUOTE: {
                printEscaped(value);
                break;
            }
            case StringStyle.UNQUOTE: {
                printer.printFastPath(value);
                break;
            }
            case StringStyle.TEXT: {
                if (settings.enableText) { // 类json模式下一定为false
                    printText(value);
                }
                else {
                    printEscaped(value);
                }
                break;
            }
            default: throw new InvalidOperationException(style.ToString());
        }
    }

    private static bool canPrintAsUnquote(string str, DsonTextWriterSettings settings) {
        return DsonTexts.canUnquoteString(str, settings.maxLengthOfUnquoteString)
               && (!settings.unicodeChar || DsonTexts.isASCIIText(str));
    }

    private static bool canPrintAsText(string str, DsonTextWriterSettings settings) {
        return settings.enableText && (str.Length > settings.softLineLength * settings.lengthFactorOfText);
    }

    /** 打印双引号String */
    private void printEscaped(String text) {
        bool unicodeChar = settings.unicodeChar;
        int softLineLength = settings.softLineLength;
        DsonPrinter printer = this.printer;
        printer.print('"');
        for (int i = 0, length = text.Length; i < length; i++) {
            printer.printEscaped(text[i], unicodeChar);
            if (printer.getColumn() >= softLineLength && (i + 1 < length)) {
                printer.println();
                printLineHead(LineHead.APPEND);
            }
        }
        printer.print('"');
    }

    /** 纯文本模式打印，要执行换行符 */
    private void printText(String text) {
        int softLineLength = settings.softLineLength;
        DsonPrinter printer = this.printer;
        printer.printFastPath("@ss "); // 开始符
        for (int i = 0, length = text.Length; i < length; i++) {
            char c = text[i];
            // 要执行文本中的换行符
            if (c == '\n') {
                printer.println();
                printLineHead(LineHead.APPEND_LINE);
                continue;
            }
            if (c == '\r' && (i + 1 < length && text[i + 1] == '\n')) {
                printer.println();
                printLineHead(LineHead.APPEND_LINE);
                i++;
                continue;
            }
            printer.print(c);
            if (printer.getColumn() > softLineLength && (i + 1 < length)) {
                printer.println();
                printLineHead(LineHead.APPEND);
            }
        }
        printer.println();
        printLineHead(LineHead.END_OF_TEXT); // 结束符
    }

    private void printBinary(byte[] buffer, int offset, int length) {
        DsonPrinter printer = this.printer;
        int softLineLength = this.settings.softLineLength;
        int segment = 8; // 分段打印控制行长度；c#的接口不能传buffer
        int loop = length / segment;
        for (int i = 0; i < loop; i++) {
            checkLineLength(printer, softLineLength, LineHead.APPEND_LINE);
            printer.printFastPath(Convert.ToHexString(buffer, offset + i * segment, segment));
        }
        int remain = length - loop * segment;
        if (remain > 0) {
            checkLineLength(printer, softLineLength, LineHead.APPEND_LINE);
            printer.printFastPath(Convert.ToHexString(buffer, offset + loop * segment, remain));
        }
    }

    private void checkLineLength(DsonPrinter printer, int softLineLength, LineHead lineHead) {
        if (printer.getColumn() >= softLineLength) {
            printer.println();
            printLineHead(lineHead);
        }
    }

    private void printLineHead(LineHead lineHead) {
        if (settings.dsonMode == DsonMode.STANDARD) {
            printer.printHead(lineHead);
        }
    }

    #endregion

    #region 简单值

    protected override void doWriteInt32(int value, WireType wireType, INumberStyle style) {
        DsonPrinter printer = this.printer;
        writeCurrentName(printer, DsonType.INT32);

        StyleOut styleOut = style.ToString(value);
        if (styleOut.IsTyped) {
            printer.printFastPath("@i ");
        }
        printer.printFastPath(styleOut.Value);
    }

    protected override void doWriteInt64(long value, WireType wireType, INumberStyle style) {
        DsonPrinter printer = this.printer;
        writeCurrentName(printer, DsonType.INT64);

        StyleOut styleOut = style.ToString(value);
        if (styleOut.IsTyped) {
            printer.printFastPath("@L ");
        }
        printer.printFastPath(styleOut.Value);
    }

    protected override void doWriteFloat(float value, INumberStyle style) {
        DsonPrinter printer = this.printer;
        writeCurrentName(printer, DsonType.FLOAT);

        StyleOut styleOut = style.ToString(value);
        if (styleOut.IsTyped) {
            printer.printFastPath("@f ");
        }
        printer.printFastPath(styleOut.Value);
    }

    protected override void doWriteDouble(double value, INumberStyle style) {
        DsonPrinter printer = this.printer;
        writeCurrentName(printer, DsonType.DOUBLE);

        StyleOut styleOut = style.ToString(value);
        if (styleOut.IsTyped) {
            printer.printFastPath("@d ");
        }
        printer.printFastPath(styleOut.Value);
    }

    protected override void doWriteBool(bool value) {
        DsonPrinter printer = this.printer;
        writeCurrentName(printer, DsonType.BOOLEAN);
        printer.printFastPath(value ? "true" : "false");
    }

    protected override void doWriteString(String value, StringStyle style) {
        DsonPrinter printer = this.printer;
        writeCurrentName(printer, DsonType.STRING);
        printString(printer, value, style);
    }

    protected override void doWriteNull() {
        DsonPrinter printer = this.printer;
        writeCurrentName(printer, DsonType.NULL);
        printer.printFastPath("null");
    }

    protected override void doWriteBinary(DsonBinary binary) {
        DsonPrinter printer = this.printer;
        writeCurrentName(printer, DsonType.BINARY);
        printer.printFastPath("[@bin ");
        printer.printFastPath(binary.Type.ToString());
        printer.printFastPath(", ");
        printBinary(binary.Data, 0, binary.Data.Length);
        printer.print(']');
    }

    protected override void doWriteBinary(int type, DsonChunk chunk) {
        DsonPrinter printer = this.printer;
        writeCurrentName(printer, DsonType.BINARY);
        printer.printFastPath("[@bin ");
        printer.printFastPath(type.ToString());
        printer.printFastPath(", ");
        printBinary(chunk.Buffer, chunk.Offset, chunk.Length);
        printer.print(']');
    }

    protected override void doWriteExtInt32(DsonExtInt32 extInt32, WireType wireType, INumberStyle style) {
        DsonPrinter printer = this.printer;
        writeCurrentName(printer, DsonType.EXT_INT32);
        printer.printFastPath("[@ei ");
        printer.printFastPath(extInt32.Type.ToString());
        printer.printFastPath(", ");
        if (extInt32.HasValue) {
            StyleOut styleOut = style.ToString(extInt32.Value);
            printer.printFastPath(styleOut.Value);
        }
        else {
            printer.printFastPath("null");
        }
        printer.print(']');
    }

    protected override void doWriteExtInt64(DsonExtInt64 extInt64, WireType wireType, INumberStyle style) {
        DsonPrinter printer = this.printer;
        writeCurrentName(printer, DsonType.EXT_INT64);
        printer.printFastPath("[@eL ");
        printer.printFastPath(extInt64.Type.ToString());
        printer.printFastPath(", ");
        if (extInt64.HasValue) {
            StyleOut styleOut = style.ToString(extInt64.Value);
            printer.printFastPath(styleOut.Value);
        }
        else {
            printer.printFastPath("null");
        }
        printer.print(']');
    }

    protected override void doWriteExtDouble(DsonExtDouble extDouble, INumberStyle style) {
        DsonPrinter printer = this.printer;
        writeCurrentName(printer, DsonType.EXT_DOUBLE);
        printer.printFastPath("[@ed ");
        printer.printFastPath(extDouble.Type.ToString());
        printer.printFastPath(", ");
        if (extDouble.HasValue) {
            StyleOut styleOut = style.ToString(extDouble.Value);
            printer.printFastPath(styleOut.Value);
        }
        else {
            printer.printFastPath("null");
        }
        printer.print(']');
    }

    protected override void doWriteExtString(DsonExtString extString, StringStyle style) {
        DsonPrinter printer = this.printer;
        writeCurrentName(printer, DsonType.EXT_STRING);
        printer.printFastPath("[@es ");
        printer.printFastPath(extString.Type.ToString());
        printer.printFastPath(", ");
        if (extString.HasValue) {
            printString(printer, extString.Value, style);
        }
        else {
            printer.printFastPath("null");
        }
        printer.print(']');
    }

    protected override void doWriteRef(ObjectRef objectRef) {
        DsonPrinter printer = this.printer;
        int softLineLength = this.settings.softLineLength;
        writeCurrentName(printer, DsonType.REFERENCE);
        if (string.IsNullOrWhiteSpace(objectRef.Ns)
            && objectRef.Type == 0 && objectRef.Policy == 0) {
            printer.printFastPath("@ref "); // 只有localId时简写
            printString(printer, objectRef.LocalId, StringStyle.AUTO_QUOTE);
            return;
        }

        printer.printFastPath("{@ref ");
        int count = 0;
        if (objectRef.hasNamespace) {
            count++;
            printer.printFastPath(ObjectRef.NAMES_NAMESPACE);
            printer.printFastPath(": ");
            printString(printer, objectRef.Ns, StringStyle.AUTO_QUOTE);
        }
        if (objectRef.hasLocalId) {
            if (count++ > 0) printer.printFastPath(", ");
            checkLineLength(printer, softLineLength, LineHead.APPEND_LINE);
            printer.printFastPath(ObjectRef.NAMES_LOCAL_ID);
            printer.printFastPath(": ");
            printString(printer, objectRef.LocalId, StringStyle.AUTO_QUOTE);
        }
        if (objectRef.Type != 0) {
            if (count++ > 0) printer.printFastPath(", ");
            checkLineLength(printer, softLineLength, LineHead.APPEND_LINE);
            printer.printFastPath(ObjectRef.NAMES_TYPE);
            printer.printFastPath(": ");
            printer.printFastPath(objectRef.Type.ToString());
        }
        if (objectRef.Policy != 0) {
            if (count > 0) printer.printFastPath(", ");
            checkLineLength(printer, softLineLength, LineHead.APPEND_LINE);
            printer.printFastPath(ObjectRef.NAMES_POLICY);
            printer.printFastPath(": ");
            printer.printFastPath(objectRef.Policy.ToString());
        }
        printer.print('}');
    }

    protected override void doWriteTimestamp(OffsetTimestamp timestamp) {
        DsonPrinter printer = this.printer;
        int softLineLength = this.settings.softLineLength;
        writeCurrentName(printer, DsonType.TIMESTAMP);
        if (timestamp.Enables == OffsetTimestamp.MASK_DATETIME) {
            printer.printFastPath("@dt ");
            printer.printFastPath(OffsetTimestamp.formatDateTime(timestamp.Seconds));
            return;
        }

        printer.printFastPath("{@dt ");
        if (timestamp.hasDate()) {
            printer.printFastPath(OffsetTimestamp.NAMES_DATE);
            printer.printFastPath(": ");
            printer.printFastPath(OffsetTimestamp.formatDate(timestamp.Seconds));
        }
        if (timestamp.hasTime()) {
            if (timestamp.hasDate()) printer.printFastPath(", ");
            checkLineLength(printer, softLineLength, LineHead.APPEND_LINE);
            printer.printFastPath(OffsetTimestamp.NAMES_TIME);
            printer.printFastPath(": ");
            printer.printFastPath(OffsetTimestamp.formatTime(timestamp.Seconds));
        }
        if (timestamp.Nanos > 0) {
            printer.printFastPath(", ");
            checkLineLength(printer, softLineLength, LineHead.APPEND_LINE);
            if (timestamp.canConvertNanosToMillis()) {
                printer.printFastPath(OffsetTimestamp.NAMES_MILLIS);
                printer.printFastPath(": ");
                printer.printFastPath(timestamp.convertNanosToMillis().ToString());
            }
            else {
                printer.printFastPath(OffsetTimestamp.NAMES_NANOS);
                printer.printFastPath(": ");
                printer.printFastPath(timestamp.Nanos.ToString());
            }
        }
        if (timestamp.hasOffset()) {
            printer.printFastPath(", ");
            checkLineLength(printer, softLineLength, LineHead.APPEND_LINE);
            printer.printFastPath(OffsetTimestamp.NAMES_OFFSET);
            printer.printFastPath(": ");
            printer.printFastPath(OffsetTimestamp.formatOffset(timestamp.Offset));
        }
        printer.print('}');
    }

    #endregion

    #region context

    private Context NewContext(Context parent, DsonContextType contextType, DsonType dsonType) {
        Context? context = GetPooledContext();
        if (context != null) {
            SetPooledContext(null);
        }
        else {
            context = new Context();
        }
        context.init(parent, contextType, dsonType);
        return context;
    }

    private void PoolContext(Context context) {
        context.reset();
        SetPooledContext(context);
    }

    protected new class Context : AbstractDsonWriter<string>.Context
    {
        internal ObjectStyle style = ObjectStyle.Indent;
        internal int headerCount = 0;
        internal int count = 0;

        public Context() {
        }

        public void reset() {
            base.reset();
            style = ObjectStyle.Indent;
            headerCount = 0;
            count = 0;
        }

        public new Context Parent => (Context)_parent;
    }

    #endregion
}