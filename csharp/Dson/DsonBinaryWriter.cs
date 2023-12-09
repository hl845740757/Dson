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

using Dson.IO;
using Dson.Text;
using Dson.Types;
using Google.Protobuf;

namespace Dson;

public class DsonBinaryWriter<TName> : AbstractDsonWriter<TName> where TName : IEquatable<TName>
{
    private IDsonOutput _output;
    private readonly AbstractDsonWriter<string>? _textWriter;
    private readonly AbstractDsonWriter<FieldNumber>? _binWriter;

    public DsonBinaryWriter(DsonWriterSettings settings, IDsonOutput output) : base(settings) {
        this._output = output;
        SetContext(new Context().Init(null, DsonContextType.TopLevel, DsonTypes.Invalid));
        if (DsonInternals.IsStringKey<TName>()) {
            _textWriter = this as AbstractDsonWriter<string>;
            _binWriter = null;
        }
        else {
            _textWriter = null;
            _binWriter = this as AbstractDsonWriter<FieldNumber>;
        }
    }

    protected override Context GetContext() {
        return (Context)_context;
    }

    protected override Context? GetPooledContext() {
        return (Context?)base.GetPooledContext();
    }

    public override void Flush() {
        _output?.Flush();
    }

    public override void Dispose() {
        if (Settings.AutoClose) {
            _output?.Dispose();
            _output = null!;
        }
        base.Dispose();
    }

    #region state

    private void WriteFullTypeAndCurrentName(IDsonOutput output, DsonType dsonType, int wireType) {
        output.WriteRawByte((byte)Dsons.MakeFullType((int)dsonType, wireType));
        if (dsonType != DsonType.Header) { // header是匿名属性
            DsonContextType contextType = this.ContextType;
            if (contextType == DsonContextType.Object || contextType == DsonContextType.Header) {
                if (_textWriter != null) { // 避免装箱
                    output.WriteString(_textWriter._context._curName);
                }
                else {
                    output.WriteUint32(_binWriter!._context._curName.FullNumber);
                }
            }
        }
    }

    #endregion

    #region 简单值

    protected override void DoWriteInt32(int value, WireType wireType, INumberStyle style) {
        IDsonOutput output = this._output;
        WriteFullTypeAndCurrentName(output, DsonType.Int32, (int)wireType);
        DsonReaderUtils.WriteInt32(output, value, wireType);
    }

    protected override void DoWriteInt64(long value, WireType wireType, INumberStyle style) {
        IDsonOutput output = this._output;
        WriteFullTypeAndCurrentName(output, DsonType.Int64, (int)wireType);
        DsonReaderUtils.WriteInt64(output, value, wireType);
    }

    protected override void DoWriteFloat(float value, INumberStyle style) {
        int wireType = DsonReaderUtils.WireTypeOfFloat(value);
        IDsonOutput output = this._output;
        WriteFullTypeAndCurrentName(output, DsonType.Float, wireType);
        DsonReaderUtils.WriteFloat(output, value, wireType);
    }

    protected override void DoWriteDouble(double value, INumberStyle style) {
        int wireType = DsonReaderUtils.WireTypeOfDouble(value);
        IDsonOutput output = this._output;
        WriteFullTypeAndCurrentName(output, DsonType.Double, wireType);
        DsonReaderUtils.WriteDouble(output, value, wireType);
    }

    protected override void DoWriteBool(bool value) {
        IDsonOutput output = this._output;
        WriteFullTypeAndCurrentName(output, DsonType.Boolean, value ? 1 : 0);
    }

    protected override void DoWriteString(String value, StringStyle style) {
        IDsonOutput output = this._output;
        WriteFullTypeAndCurrentName(output, DsonType.String, 0);
        output.WriteString(value);
    }

    protected override void DoWriteNull() {
        IDsonOutput output = this._output;
        WriteFullTypeAndCurrentName(output, DsonType.Null, 0);
    }

    protected override void DoWriteBinary(DsonBinary binary) {
        IDsonOutput output = this._output;
        WriteFullTypeAndCurrentName(output, DsonType.Binary, 0);
        DsonReaderUtils.WriteBinary(output, binary);
    }

    protected override void DoWriteBinary(int type, DsonChunk chunk) {
        IDsonOutput output = this._output;
        WriteFullTypeAndCurrentName(output, DsonType.Binary, 0);
        DsonReaderUtils.WriteBinary(output, type, chunk);
    }

    protected override void DoWriteExtInt32(DsonExtInt32 extInt32, WireType wireType, INumberStyle style) {
        IDsonOutput output = this._output;
        WriteFullTypeAndCurrentName(output, DsonType.ExtInt32, (int)wireType);
        DsonReaderUtils.WriteExtInt32(output, extInt32, wireType);
    }

    protected override void DoWriteExtInt64(DsonExtInt64 extInt64, WireType wireType, INumberStyle style) {
        IDsonOutput output = this._output;
        WriteFullTypeAndCurrentName(output, DsonType.ExtInt64, (int)wireType);
        DsonReaderUtils.WriteExtInt64(output, extInt64, wireType);
    }

    protected override void DoWriteExtDouble(DsonExtDouble extDouble, INumberStyle style) {
        int wireType = DsonReaderUtils.WireTypeOfDouble(extDouble.Value);
        IDsonOutput output = this._output;
        WriteFullTypeAndCurrentName(output, DsonType.ExtDouble, wireType);
        DsonReaderUtils.WriteExtDouble(output, extDouble, wireType);
    }

    protected override void DoWriteExtString(DsonExtString extString, StringStyle style) {
        IDsonOutput output = this._output;
        WriteFullTypeAndCurrentName(output, DsonType.ExtString, DsonReaderUtils.WireTypeOfExtString(extString));
        DsonReaderUtils.WriteExtString(output, extString);
    }

    protected override void DoWriteRef(ObjectRef objectRef) {
        IDsonOutput output = this._output;
        WriteFullTypeAndCurrentName(output, DsonType.Reference, DsonReaderUtils.WireTypeOfRef(objectRef));
        DsonReaderUtils.WriteRef(output, objectRef);
    }

    protected override void DoWriteTimestamp(OffsetTimestamp timestamp) {
        IDsonOutput output = this._output;
        WriteFullTypeAndCurrentName(output, DsonType.Timestamp, 0);
        DsonReaderUtils.WriteTimestamp(output, timestamp);
    }

    #endregion

    #region 容器

    protected override void DoWriteStartContainer(DsonContextType contextType, DsonType dsonType, ObjectStyle style) {
        IDsonOutput output = this._output;
        WriteFullTypeAndCurrentName(output, dsonType, 0);

        Context newContext = NewContext(GetContext(), contextType, dsonType);
        newContext._preWritten = output.Position;
        output.WriteFixed32(0);

        SetContext(newContext);
        this._recursionDepth++;
    }

    protected override void DoWriteEndContainer() {
        // 记录preWritten在写length之前，最后的size要减4
        Context context = GetContext();
        int preWritten = context._preWritten;
        _output.SetFixedInt32(preWritten, _output.Position - preWritten - 4);

        this._recursionDepth--;
        SetContext(context.Parent);
        PoolContext(context);
    }

    #endregion

    #region 特殊

    protected override void DoWriteMessage(int binaryType, IMessage messageLite) {
        IDsonOutput output = this._output;
        WriteFullTypeAndCurrentName(output, DsonType.Binary, 0);
        DsonReaderUtils.WriteMessage(output, binaryType, messageLite);
    }

    protected override void DoWriteValueBytes(DsonType type, byte[] data) {
        IDsonOutput output = this._output;
        WriteFullTypeAndCurrentName(output, type, 0);
        DsonReaderUtils.WriteValueBytes(output, type, data);
    }

    #endregion

    #region context

    private Context NewContext(Context parent, DsonContextType contextType, DsonType dsonType) {
        Context context = GetPooledContext();
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

    protected new class Context : AbstractDsonWriter<TName>.Context
    {
        protected internal int _preWritten = 0;

        public Context() {
        }

        public override void Reset() {
            base.Reset();
            _preWritten = 0;
        }
    }

    #endregion
}