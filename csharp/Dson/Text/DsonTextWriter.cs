#region LICENSE

//  Copyright 2023 wjybxx
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

using System.Diagnostics;
using Wjybxx.Dson.IO;
using Wjybxx.Dson.Types;
using Google.Protobuf;

namespace Wjybxx.Dson.Text;

public class DsonTextWriter : AbstractDsonWriter<string>
{
#nullable disable
    private readonly TextWriter _writer;
    private readonly DsonTextWriterSettings _settings;
    private DsonPrinter _printer;
#nullable enable

    public DsonTextWriter(DsonTextWriterSettings settings, TextWriter writer)
        : base(settings) {
        this._settings = settings;
        this._writer = writer;
        this._printer = new DsonPrinter(writer, settings.LineSeparator, settings.AutoClose);
        SetContext(new Context().Init(null, DsonContextType.TopLevel, DsonTypes.Invalid));
    }

    public TextWriter StreamWriter => _writer ?? throw new ObjectDisposedException("writer");

    protected override Context GetContext() {
        return (Context)_context;
    }

    protected override Context? GetPooledContext() {
        return (Context?)base.GetPooledContext();
    }

    public override void Flush() {
        _printer?.Flush();
    }

    public override void Dispose() {
        if (Settings.AutoClose) {
            _printer?.Dispose();
            _printer = null!;
        }
        base.Dispose();
    }

    #region state

    private void WriteCurrentName(DsonPrinter printer, DsonType dsonType) {
        Context context = GetContext();
        // 打印元素前先检查是否打印了行首
        if (printer.HeadLabel == null) {
            PrintLineHead(LineHead.AppendLine);
        }
        // header与外层对象无缩进，且是匿名属性 -- 如果打印多个header，将保持连续
        if (dsonType == DsonType.Header) {
            Debug.Assert(context._count == 0);
            context._headerCount++;
            return;
        }
        // 处理value之间分隔符
        if (context._count > 0) {
            printer.PrintFastPath(",");
        }
        // 先处理长度超出，再处理缩进
        if (printer.Column >= _settings.SoftLineLength) {
            printer.Println();
            PrintLineHead(LineHead.AppendLine);
        }
        if (context._style == ObjectStyle.Indent) {
            if (context._count > 0 && printer.ContentLength < printer.IndentLength()) {
                // 当前字符数小于缩进也不换行，但首个字段需要换行
                printer.PrintIndent(printer.ContentLength);
            }
            else if (printer.HasContent) {
                // 当前行有内容了才换行缩进
                printer.Println();
                PrintLineHead(LineHead.AppendLine);
                printer.PrintIndent();
            }
        }
        else if (context._count > 0) {
            // 非缩进模式下，元素之间打印一个空格
            printer.Print(' ');
        }
        if (context._contextType.IsLikeObject()) {
            PrintString(printer, context._curName, StringStyle.AutoQuote);
            printer.PrintFastPath(": ");
        }
        context._count++;
    }

    private void PrintString(DsonPrinter printer, string value, StringStyle style) {
        DsonTextWriterSettings settings = this._settings;
        switch (style) {
            case StringStyle.Auto: {
                if (CanPrintAsUnquote(value, settings)) {
                    printer.PrintFastPath(value);
                }
                else if (CanPrintAsText(value, settings)) {
                    PrintText(value);
                }
                else {
                    PrintEscaped(value);
                }
                break;
            }
            case StringStyle.AutoQuote: {
                if (CanPrintAsUnquote(value, settings)) {
                    printer.PrintFastPath(value);
                }
                else {
                    PrintEscaped(value);
                }
                break;
            }
            case StringStyle.Quote: {
                PrintEscaped(value);
                break;
            }
            case StringStyle.Unquote: {
                printer.PrintFastPath(value);
                break;
            }
            case StringStyle.Text: {
                if (settings.EnableText) { // 类json模式下一定为false
                    PrintText(value);
                }
                else {
                    PrintEscaped(value);
                }
                break;
            }
            default: throw new InvalidOperationException(style.ToString());
        }
    }

    private static bool CanPrintAsUnquote(string str, DsonTextWriterSettings settings) {
        return DsonTexts.CanUnquoteString(str, settings.MaxLengthOfUnquoteString)
               && (!settings.UnicodeChar || DsonTexts.IsAsciiText(str));
    }

    private static bool CanPrintAsText(string str, DsonTextWriterSettings settings) {
        return settings.EnableText && (str.Length > settings.SoftLineLength * settings.LengthFactorOfText);
    }

    /** 打印双引号String */
    private void PrintEscaped(string text) {
        bool unicodeChar = _settings.UnicodeChar;
        int softLineLength = _settings.SoftLineLength;
        DsonPrinter printer = this._printer;
        printer.Print('"');
        for (int i = 0, length = text.Length; i < length; i++) {
            printer.PrintEscaped(text[i], unicodeChar);
            if (printer.Column >= softLineLength && (i + 1 < length)) {
                printer.Println();
                PrintLineHead(LineHead.Append);
            }
        }
        printer.Print('"');
    }

    /** 纯文本模式打印，要执行换行符 */
    private void PrintText(string text) {
        int softLineLength = _settings.SoftLineLength;
        DsonPrinter printer = this._printer;
        printer.PrintFastPath("@ss "); // 开始符
        for (int i = 0, length = text.Length; i < length; i++) {
            char c = text[i];
            // 要执行文本中的换行符
            if (c == '\n') {
                printer.Println();
                PrintLineHead(LineHead.AppendLine);
                continue;
            }
            if (c == '\r' && (i + 1 < length && text[i + 1] == '\n')) {
                printer.Println();
                PrintLineHead(LineHead.AppendLine);
                i++;
                continue;
            }
            printer.Print(c);
            if (printer.Column > softLineLength && (i + 1 < length)) {
                printer.Println();
                PrintLineHead(LineHead.Append);
            }
        }
        printer.Println();
        PrintLineHead(LineHead.EndOfText); // 结束符
    }

    private void PrintBinary(byte[] buffer, int offset, int length) {
        DsonPrinter printer = this._printer;
        int softLineLength = this._settings.SoftLineLength;
        int segment = 8; // 分段打印控制行长度；c#的接口不能传buffer
        int loop = length / segment;
        for (int i = 0; i < loop; i++) {
            CheckLineLength(printer, softLineLength, LineHead.AppendLine);
            printer.PrintFastPath(Convert.ToHexString(buffer, offset + i * segment, segment));
        }
        int remain = length - loop * segment;
        if (remain > 0) {
            CheckLineLength(printer, softLineLength, LineHead.AppendLine);
            printer.PrintFastPath(Convert.ToHexString(buffer, offset + loop * segment, remain));
        }
    }

    private void CheckLineLength(DsonPrinter printer, int softLineLength, LineHead lineHead) {
        if (printer.Column >= softLineLength) {
            printer.Println();
            PrintLineHead(lineHead);
        }
    }

    private void PrintLineHead(LineHead lineHead) {
        if (_settings.DsonMode == DsonMode.Standard) {
            _printer.PrintHead(lineHead);
        }
    }

    #endregion

    #region 简单值

    protected override void DoWriteInt32(int value, WireType wireType, INumberStyle style) {
        DsonPrinter printer = this._printer;
        WriteCurrentName(printer, DsonType.Int32);

        StyleOut styleOut = style.ToString(value);
        if (styleOut.IsTyped) {
            printer.PrintFastPath("@i ");
        }
        printer.PrintFastPath(styleOut.Value);
    }

    protected override void DoWriteInt64(long value, WireType wireType, INumberStyle style) {
        DsonPrinter printer = this._printer;
        WriteCurrentName(printer, DsonType.Int64);

        StyleOut styleOut = style.ToString(value);
        if (styleOut.IsTyped) {
            printer.PrintFastPath("@L ");
        }
        printer.PrintFastPath(styleOut.Value);
    }

    protected override void DoWriteFloat(float value, INumberStyle style) {
        DsonPrinter printer = this._printer;
        WriteCurrentName(printer, DsonType.Float);

        StyleOut styleOut = style.ToString(value);
        if (styleOut.IsTyped) {
            printer.PrintFastPath("@f ");
        }
        printer.PrintFastPath(styleOut.Value);
    }

    protected override void DoWriteDouble(double value, INumberStyle style) {
        DsonPrinter printer = this._printer;
        WriteCurrentName(printer, DsonType.Double);

        StyleOut styleOut = style.ToString(value);
        if (styleOut.IsTyped) {
            printer.PrintFastPath("@d ");
        }
        printer.PrintFastPath(styleOut.Value);
    }

    protected override void DoWriteBool(bool value) {
        DsonPrinter printer = this._printer;
        WriteCurrentName(printer, DsonType.Boolean);
        printer.PrintFastPath(value ? "true" : "false");
    }

    protected override void DoWriteString(string value, StringStyle style) {
        DsonPrinter printer = this._printer;
        WriteCurrentName(printer, DsonType.String);
        PrintString(printer, value, style);
    }

    protected override void DoWriteNull() {
        DsonPrinter printer = this._printer;
        WriteCurrentName(printer, DsonType.Null);
        printer.PrintFastPath("null");
    }

    protected override void DoWriteBinary(DsonBinary binary) {
        DsonPrinter printer = this._printer;
        WriteCurrentName(printer, DsonType.Binary);
        printer.PrintFastPath("[@bin ");
        printer.PrintFastPath(binary.Type.ToString());
        printer.PrintFastPath(", ");
        PrintBinary(binary.Data, 0, binary.Data.Length);
        printer.Print(']');
    }

    protected override void DoWriteBinary(int type, DsonChunk chunk) {
        DsonPrinter printer = this._printer;
        WriteCurrentName(printer, DsonType.Binary);
        printer.PrintFastPath("[@bin ");
        printer.PrintFastPath(type.ToString());
        printer.PrintFastPath(", ");
        PrintBinary(chunk.Buffer, chunk.Offset, chunk.Length);
        printer.Print(']');
    }

    protected override void DoWriteExtInt32(DsonExtInt32 extInt32, WireType wireType, INumberStyle style) {
        DsonPrinter printer = this._printer;
        WriteCurrentName(printer, DsonType.ExtInt32);
        printer.PrintFastPath("[@ei ");
        printer.PrintFastPath(extInt32.Type.ToString());
        printer.PrintFastPath(", ");
        if (extInt32.HasValue) {
            StyleOut styleOut = style.ToString(extInt32.Value);
            printer.PrintFastPath(styleOut.Value);
        }
        else {
            printer.PrintFastPath("null");
        }
        printer.Print(']');
    }

    protected override void DoWriteExtInt64(DsonExtInt64 extInt64, WireType wireType, INumberStyle style) {
        DsonPrinter printer = this._printer;
        WriteCurrentName(printer, DsonType.ExtInt64);
        printer.PrintFastPath("[@eL ");
        printer.PrintFastPath(extInt64.Type.ToString());
        printer.PrintFastPath(", ");
        if (extInt64.HasValue) {
            StyleOut styleOut = style.ToString(extInt64.Value);
            printer.PrintFastPath(styleOut.Value);
        }
        else {
            printer.PrintFastPath("null");
        }
        printer.Print(']');
    }

    protected override void DoWriteExtDouble(DsonExtDouble extDouble, INumberStyle style) {
        DsonPrinter printer = this._printer;
        WriteCurrentName(printer, DsonType.ExtDouble);
        printer.PrintFastPath("[@ed ");
        printer.PrintFastPath(extDouble.Type.ToString());
        printer.PrintFastPath(", ");
        if (extDouble.HasValue) {
            StyleOut styleOut = style.ToString(extDouble.Value);
            printer.PrintFastPath(styleOut.Value);
        }
        else {
            printer.PrintFastPath("null");
        }
        printer.Print(']');
    }

    protected override void DoWriteExtString(DsonExtString extString, StringStyle style) {
        DsonPrinter printer = this._printer;
        WriteCurrentName(printer, DsonType.ExtString);
        printer.PrintFastPath("[@es ");
        printer.PrintFastPath(extString.Type.ToString());
        printer.PrintFastPath(", ");
        if (extString.HasValue) {
            PrintString(printer, extString.Value!, style);
        }
        else {
            printer.PrintFastPath("null");
        }
        printer.Print(']');
    }

    protected override void DoWriteRef(ObjectRef objectRef) {
        DsonPrinter printer = this._printer;
        int softLineLength = this._settings.SoftLineLength;
        WriteCurrentName(printer, DsonType.Reference);
        if (string.IsNullOrWhiteSpace(objectRef.Ns)
            && objectRef.Type == 0 && objectRef.Policy == 0) {
            printer.PrintFastPath("@ref "); // 只有localId时简写
            PrintString(printer, objectRef.LocalId, StringStyle.AutoQuote);
            return;
        }

        printer.PrintFastPath("{@ref ");
        int count = 0;
        if (objectRef.hasNamespace) {
            count++;
            printer.PrintFastPath(ObjectRef.NamesNamespace);
            printer.PrintFastPath(": ");
            PrintString(printer, objectRef.Ns, StringStyle.AutoQuote);
        }
        if (objectRef.hasLocalId) {
            if (count++ > 0) printer.PrintFastPath(", ");
            CheckLineLength(printer, softLineLength, LineHead.AppendLine);
            printer.PrintFastPath(ObjectRef.NamesLocalId);
            printer.PrintFastPath(": ");
            PrintString(printer, objectRef.LocalId, StringStyle.AutoQuote);
        }
        if (objectRef.Type != 0) {
            if (count++ > 0) printer.PrintFastPath(", ");
            CheckLineLength(printer, softLineLength, LineHead.AppendLine);
            printer.PrintFastPath(ObjectRef.NamesType);
            printer.PrintFastPath(": ");
            printer.PrintFastPath(objectRef.Type.ToString());
        }
        if (objectRef.Policy != 0) {
            if (count > 0) printer.PrintFastPath(", ");
            CheckLineLength(printer, softLineLength, LineHead.AppendLine);
            printer.PrintFastPath(ObjectRef.NamesPolicy);
            printer.PrintFastPath(": ");
            printer.PrintFastPath(objectRef.Policy.ToString());
        }
        printer.Print('}');
    }

    protected override void DoWriteTimestamp(OffsetTimestamp timestamp) {
        DsonPrinter printer = this._printer;
        int softLineLength = this._settings.SoftLineLength;
        WriteCurrentName(printer, DsonType.Timestamp);
        if (timestamp.Enables == OffsetTimestamp.MaskDatetime) {
            printer.PrintFastPath("@dt ");
            printer.PrintFastPath(OffsetTimestamp.FormatDateTime(timestamp.Seconds));
            return;
        }

        printer.PrintFastPath("{@dt ");
        if (timestamp.HasDate) {
            printer.PrintFastPath(OffsetTimestamp.NamesDate);
            printer.PrintFastPath(": ");
            printer.PrintFastPath(OffsetTimestamp.FormatDate(timestamp.Seconds));
        }
        if (timestamp.HasTime) {
            if (timestamp.HasDate) printer.PrintFastPath(", ");
            CheckLineLength(printer, softLineLength, LineHead.AppendLine);
            printer.PrintFastPath(OffsetTimestamp.NamesTime);
            printer.PrintFastPath(": ");
            printer.PrintFastPath(OffsetTimestamp.FormatTime(timestamp.Seconds));
        }
        if (timestamp.Nanos > 0) {
            printer.PrintFastPath(", ");
            CheckLineLength(printer, softLineLength, LineHead.AppendLine);
            if (timestamp.CanConvertNanosToMillis()) {
                printer.PrintFastPath(OffsetTimestamp.NamesMillis);
                printer.PrintFastPath(": ");
                printer.PrintFastPath(timestamp.ConvertNanosToMillis().ToString());
            }
            else {
                printer.PrintFastPath(OffsetTimestamp.NamesNanos);
                printer.PrintFastPath(": ");
                printer.PrintFastPath(timestamp.Nanos.ToString());
            }
        }
        if (timestamp.HasOffset) {
            printer.PrintFastPath(", ");
            CheckLineLength(printer, softLineLength, LineHead.AppendLine);
            printer.PrintFastPath(OffsetTimestamp.NamesOffset);
            printer.PrintFastPath(": ");
            printer.PrintFastPath(OffsetTimestamp.FormatOffset(timestamp.Offset));
        }
        printer.Print('}');
    }

    #endregion

    #region 容器

    protected override void DoWriteStartContainer(DsonContextType contextType, DsonType dsonType, ObjectStyle style) {
        DsonPrinter printer = this._printer;
        WriteCurrentName(printer, dsonType);

        Context newContext = NewContext(GetContext(), contextType, dsonType);
        newContext._style = style;

        printer.PrintFastPath(contextType.GetStartSymbol()!);
        if (style == ObjectStyle.Indent) {
            printer.Indent(); // 调整缩进
        }

        SetContext(newContext);
        this._recursionDepth++;
    }

    protected override void DoWriteEndContainer() {
        Context context = GetContext();
        DsonPrinter printer = this._printer;

        if (context._style == ObjectStyle.Indent) {
            printer.Retract(); // 恢复缩进
            // 打印了内容的情况下才换行结束
            if (printer.HasContent && (context._headerCount > 0 || context._count > 0)) {
                printer.Println();
                PrintLineHead(LineHead.AppendLine);
                printer.PrintIndent();
            }
        }
        printer.PrintFastPath(context._contextType.GetEndSymbol()!);

        this._recursionDepth--;
        SetContext(context.Parent);
        PoolContext(context);
    }

    #endregion

    #region 特殊

    public override void WriteSimpleHeader(string clsName) {
        if (clsName == null) throw new ArgumentNullException(nameof(clsName));
        Context context = GetContext();
        if (!CanPrintAsUnquote(clsName, _settings)) {
            base.WriteSimpleHeader(clsName);
            return;
        }
        if (context._contextType == DsonContextType.Object && context._state == DsonWriterState.Name) {
            context.SetState(DsonWriterState.Value);
        }
        AutoStartTopLevel(context);
        EnsureValueState(context);

        DsonPrinter printer = this._printer;
        WriteCurrentName(printer, DsonType.Header);

        printer.Print('@');
        printer.PrintFastPath(clsName);
        if (context._style != ObjectStyle.Indent) {
            printer.Print(' ');
        }
        SetNextState();
    }

    protected override void DoWriteMessage(int binaryType, IMessage message) {
        DoWriteBinary(new DsonBinary(binaryType, message.ToByteArray()));
    }

    protected override void DoWriteValueBytes(DsonType type, byte[] data) {
        throw new InvalidOperationException("UnsupportedOperation");
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
        context.Init(parent, contextType, dsonType);
        return context;
    }

    private void PoolContext(Context context) {
        context.Reset();
        SetPooledContext(context);
    }

    protected new class Context : AbstractDsonWriter<string>.Context
    {
        internal ObjectStyle _style = ObjectStyle.Indent;
        internal int _headerCount = 0;
        internal int _count = 0;

        public Context() {
        }

        public override void Reset() {
            base.Reset();
            _style = ObjectStyle.Indent;
            _headerCount = 0;
            _count = 0;
        }

        public new Context Parent => (Context)_parent;
    }

    #endregion
}