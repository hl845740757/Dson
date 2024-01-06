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
using System.Diagnostics;
using System.IO;
using Wjybxx.Dson.Internal;
using Wjybxx.Dson.IO;
using Wjybxx.Dson.Types;

#pragma warning disable CS1591
namespace Wjybxx.Dson.Text;

/// <summary>
/// 将输出写为文本的Writer
/// </summary>
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
        this._writer = writer ?? throw new ArgumentNullException(nameof(writer));
        this._printer = new DsonPrinter(settings, writer);
        SetContext(new Context().Init(null, DsonContextType.TopLevel, DsonTypes.Invalid));
    }

    public TextWriter StreamWriter => _writer ?? throw new ObjectDisposedException("writer");

    protected override Context GetContext() {
        return (Context)_context;
    }

    protected override AbstractDsonWriter<string>.Context NewContext() {
        return new Context();
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
        bool indented = false;
        if (printer.Column == 0) {
            indented = true;
            PrintLineHead(LineHead.AppendLine);
        }
        // header与外层对象无缩进，且是匿名属性 -- 如果打印多个header，将保持连续
        if (dsonType == DsonType.Header) {
            Debug.Assert(context.count == 0);
            context.headerCount++;
            return;
        }
        // 处理value之间分隔符
        if (context.count > 0) {
            printer.Print(',');
        }
        // 先处理长度超出，再处理缩进
        if (printer.Column >= _settings.SoftLineLength) {
            indented = true;
            printer.Println();
            PrintLineHead(LineHead.AppendLine);
        }
        if (!indented) {
            if (context.style == ObjectStyle.Indent) {
                if (context.HasElement() && printer.Column < printer.PrettyBodyColum) {
                    // 当前行是字符串结束行，字符串结束位置尚未到达缩进，不换行
                    printer.PrintSpaces(printer.PrettyBodyColum - printer.Column);
                } else if (context.count == 0 || printer.Column > printer.PrettyBodyColum) {
                    // 当前行有内容了才换行缩进；首个元素需要缩进
                    printer.Println();
                    PrintLineHead(LineHead.AppendLine);
                    printer.PrintBodyIndent();
                }
            } else if (context.HasElement()) {
                // 非缩进模式下，元素之间打印一个空格
                printer.PrintFastPath(' ');
            }
        }
        if (context.contextType.IsLikeObject()) {
            PrintString(printer, context.curName, StringStyle.AutoQuote);
            printer.Print(": ");
        }
        context.count++;
    }

    private void PrintString(DsonPrinter printer, string value, StringStyle style) {
        DsonTextWriterSettings settings = this._settings;
        switch (style) {
            case StringStyle.Auto: {
                if (CanPrintAsUnquote(value, settings)) {
                    printer.PrintFastPath(value);
                } else if (CanPrintAsText(value, settings)) {
                    PrintText(value);
                } else {
                    PrintEscaped(value);
                }
                break;
            }
            case StringStyle.AutoQuote: {
                if (CanPrintAsUnquote(value, settings)) {
                    printer.PrintFastPath(value);
                } else {
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
                } else {
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
        return settings.EnableText && (str.Length > settings.TextStringLength);
    }

    /** 打印双引号String */
    private void PrintEscaped(string text) {
        bool unicodeChar = _settings.UnicodeChar;
        int softLineLength = _settings.SoftLineLength;
        DsonPrinter printer = this._printer;
        int headIndent;
        if (_settings.DsonMode == DsonMode.Standard) {
            headIndent = _settings.StringAlignLeft ? printer.PrettyBodyColum : _settings.ExtraIndent;
        } else {
            headIndent = 0; // 字符串不能在非标准模式下缩进
        }
        printer.Print('"');
        for (int i = 0, length = text.Length; i < length; i++) {
            char c = text[i];
            if (char.IsSurrogate(c)) {
                printer.PrintHpmCodePoint(c, text[++i]);
            } else {
                printer.PrintEscaped(c, unicodeChar);
            }
            if (printer.Column > softLineLength && (i + 1 < length)) {
                printer.Println();
                printer.HeadIndent = headIndent;
                printer.BodyIndent = 0;
                PrintLineHead(LineHead.Append);
            }
        }
        printer.Print('"');
    }

    /** 纯文本模式打印，要执行换行符 */
    private void PrintText(string text) {
        int softLineLength = _settings.SoftLineLength;
        DsonPrinter printer = this._printer;
        int headIndent;
        if (_settings.TextAlignLeft) {
            headIndent = printer.PrettyBodyColum;
            printer.PrintFastPath("@ss"); // 开始符
            printer.Println();
            printer.HeadIndent = headIndent;
            printer.BodyIndent = 0;
            PrintLineHead(LineHead.Append);
        } else {
            headIndent = _settings.ExtraIndent;
            printer.PrintFastPath("@ss "); // 开始符
        }
        for (int i = 0, length = text.Length; i < length; i++) {
            char c = text[i];
            // 要执行文本中的换行符
            if (c == '\n') {
                printer.Println();
                printer.HeadIndent = headIndent;
                printer.BodyIndent = 0;
                PrintLineHead(LineHead.AppendLine);
                continue;
            }
            if (c == '\r' && (i + 1 < length && text[i + 1] == '\n')) {
                printer.Println();
                printer.HeadIndent = headIndent;
                printer.BodyIndent = 0;
                PrintLineHead(LineHead.AppendLine);
                i++;
                continue;
            }
            if (char.IsSurrogate(c)) {
                printer.PrintHpmCodePoint(c, text[++i]);
            } else {
                printer.Print(c);
            }
            if (printer.Column > softLineLength && (i + 1 < length)) {
                printer.Println();
                printer.HeadIndent = headIndent;
                printer.BodyIndent = 0;
                PrintLineHead(LineHead.Append);
            }
        }
        printer.Println();
        printer.HeadIndent = headIndent;
        PrintLineHead(LineHead.EndOfText); // 结束符
    }

    private void PrintBinary(byte[] buffer, int offset, int length) {
        DsonPrinter printer = this._printer;
        int softLineLength = this._settings.SoftLineLength;

        // 使用小buffer多次编码代替大的buffer，一方面节省内存，一方面控制行长度
        int segment = 8;
        Span<char> cBuffer = stackalloc char[segment * 2];
        int loop = length / segment;
        for (int i = 0; i < loop; i++) {
            CheckLineLength(printer, softLineLength, LineHead.AppendLine);
            CommonsLang3.EncodeHex(buffer, offset + i * segment, segment, cBuffer);
            printer.PrintFastPath(cBuffer);
        }
        int remain = length - loop * segment;
        if (remain > 0) {
            CheckLineLength(printer, softLineLength, LineHead.AppendLine);
            CommonsLang3.EncodeHex(buffer, offset + loop * segment, remain, cBuffer);
            printer.PrintFastPath(cBuffer.Slice(0, remain * 2));
        }
    }

    private void CheckLineLength(DsonPrinter printer, int softLineLength, LineHead lineHead) {
        if (printer.Column >= softLineLength) {
            printer.Println();
            PrintLineHead(lineHead);
        }
    }

    private void PrintLineHead(LineHead lineHead) {
        _printer.PrintHeadIndent();
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

    protected override void DoWriteBinary(Binary binary) {
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

    protected override void DoWriteExtInt32(ExtInt32 extInt32, WireType wireType, INumberStyle style) {
        DsonPrinter printer = this._printer;
        WriteCurrentName(printer, DsonType.ExtInt32);
        printer.PrintFastPath("[@ei ");
        printer.PrintFastPath(extInt32.Type.ToString());
        printer.PrintFastPath(", ");
        if (extInt32.HasValue) {
            StyleOut styleOut = style.ToString(extInt32.Value);
            printer.PrintFastPath(styleOut.Value);
        } else {
            printer.PrintFastPath("null");
        }
        printer.Print(']');
    }

    protected override void DoWriteExtInt64(ExtInt64 extInt64, WireType wireType, INumberStyle style) {
        DsonPrinter printer = this._printer;
        WriteCurrentName(printer, DsonType.ExtInt64);
        printer.PrintFastPath("[@eL ");
        printer.PrintFastPath(extInt64.Type.ToString());
        printer.PrintFastPath(", ");
        if (extInt64.HasValue) {
            StyleOut styleOut = style.ToString(extInt64.Value);
            printer.PrintFastPath(styleOut.Value);
        } else {
            printer.PrintFastPath("null");
        }
        printer.Print(']');
    }

    protected override void DoWriteExtDouble(ExtDouble extDouble, INumberStyle style) {
        DsonPrinter printer = this._printer;
        WriteCurrentName(printer, DsonType.ExtDouble);
        printer.PrintFastPath("[@ed ");
        printer.PrintFastPath(extDouble.Type.ToString());
        printer.PrintFastPath(", ");
        if (extDouble.HasValue) {
            StyleOut styleOut = style.ToString(extDouble.Value);
            printer.PrintFastPath(styleOut.Value);
        } else {
            printer.PrintFastPath("null");
        }
        printer.Print(']');
    }

    protected override void DoWriteExtString(ExtString extString, StringStyle style) {
        DsonPrinter printer = this._printer;
        WriteCurrentName(printer, DsonType.ExtString);
        printer.PrintFastPath("[@es ");
        printer.PrintFastPath(extString.Type.ToString());
        printer.PrintFastPath(", ");
        if (extString.HasValue) {
            PrintString(printer, extString.Value!, style);
        } else {
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
            } else {
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

        Context context = GetContext();
        if (context.style == ObjectStyle.Flow) {
            style = ObjectStyle.Flow;
        }
        Context newContext = NewContext(context, contextType, dsonType);
        newContext.style = style;

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

        if (context.style == ObjectStyle.Indent) {
            printer.Retract(); // 恢复缩进
            // 打印了内容的情况下才换行结束
            if (context.HasElement() && (printer.Column > printer.PrettyBodyColum)) {
                printer.Println();
                PrintLineHead(LineHead.AppendLine);
                printer.PrintBodyIndent();
            }
        }
        printer.PrintFastPath(context.contextType.GetEndSymbol()!);

        this._recursionDepth--;
        SetContext(context.Parent);
        ReturnContext(context);
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
        if (context.contextType == DsonContextType.Object && context.state == DsonWriterState.Name) {
            context.SetState(DsonWriterState.Value);
        }
        AutoStartTopLevel(context);
        EnsureValueState(context);

        DsonPrinter printer = this._printer;
        WriteCurrentName(printer, DsonType.Header);
        // header总是使用 @{} 包起来，提高辨识度
        printer.Print("@{");
        PrintString(printer, clsName, StringStyle.Auto);
        printer.Print('}');
        SetNextState();
    }

    protected override void DoWriteValueBytes(DsonType type, byte[] data) {
        throw new InvalidOperationException("UnsupportedOperation");
    }

    #endregion

    #region context

    private Context NewContext(Context parent, DsonContextType contextType, DsonType dsonType) {
        Context context = (Context)RentContext();
        context.Init(parent, contextType, dsonType);
        return context;
    }

    protected new class Context : AbstractDsonWriter<string>.Context
    {
        internal ObjectStyle style = ObjectStyle.Indent;
        internal int headerCount = 0;
        internal int count = 0;

        public Context() {
        }

        internal bool HasElement() {
            return headerCount > 0 || count > 0;
        }

        public override void Reset() {
            base.Reset();
            style = ObjectStyle.Indent;
            headerCount = 0;
            count = 0;
        }

        public new Context Parent => (Context)parent;
    }

    #endregion
}