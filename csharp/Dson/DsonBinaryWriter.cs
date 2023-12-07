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

using Dson.IO;
using Dson.Text;
using Dson.Types;
using Google.Protobuf;

namespace Dson;

public class DsonBinaryWriter<TName> : AbstractDsonWriter<TName> where TName : IEquatable<TName>
{
    private IDsonOutput _output;

    public DsonBinaryWriter(DsonWriterSettings settings, IDsonOutput output) : base(settings) {
        this._output = output;
    }

    protected override Context GetContext() {
        return (Context)_context;
    }

    protected override Context? GetPooledContext() {
        return (Context)base.GetPooledContext();
    }

    public override void Flush() {
        _output?.Flush();
    }

    public override void Dispose() {
        if (Settings.autoClose) {
            _output?.Dispose();
            _output = null!;
        }
        base.Dispose();
    }

    #region state

    private void WriteFullTypeAndCurrentName(IDsonOutput output, DsonType dsonType, int wireType) {
        output.WriteRawByte((byte)Dsons.MakeFullType((int)dsonType, wireType));
        if (dsonType != DsonType.HEADER) { // header是匿名属性
            DsonContextType contextType = this.ContextType;
            if (contextType == DsonContextType.OBJECT || contextType == DsonContextType.HEADER) {
                if (TextWriter != null) { // 避免装箱
                    output.WriteString(TextWriter._context.curName);
                }
                else {
                    output.WriteUint32(BinWriter!._context.curName);
                }
            }
        }
    }

    #endregion

    #region 简单值

    protected override void doWriteInt32(int value, WireType wireType, INumberStyle style) {
        IDsonOutput output = this._output;
        WriteFullTypeAndCurrentName(output, DsonType.INT32, (int)wireType);
        DsonReaderUtils.writeInt32(output, value, wireType);
    }

    protected override void doWriteInt64(long value, WireType wireType, INumberStyle style) {
        IDsonOutput output = this._output;
        WriteFullTypeAndCurrentName(output, DsonType.INT64, (int)wireType);
        DsonReaderUtils.writeInt64(output, value, wireType);
    }

    protected override void doWriteFloat(float value, INumberStyle style) {
        int wireType = DsonReaderUtils.wireTypeOfFloat(value);
        IDsonOutput output = this._output;
        WriteFullTypeAndCurrentName(output, DsonType.FLOAT, wireType);
        DsonReaderUtils.writeFloat(output, value, wireType);
    }

    protected override void doWriteDouble(double value, INumberStyle style) {
        int wireType = DsonReaderUtils.wireTypeOfDouble(value);
        IDsonOutput output = this._output;
        WriteFullTypeAndCurrentName(output, DsonType.DOUBLE, wireType);
        DsonReaderUtils.writeDouble(output, value, wireType);
    }

    protected override void doWriteBool(bool value) {
        IDsonOutput output = this._output;
        WriteFullTypeAndCurrentName(output, DsonType.BOOLEAN, value ? 1 : 0);
    }

    protected override void doWriteString(String value, StringStyle style) {
        IDsonOutput output = this._output;
        WriteFullTypeAndCurrentName(output, DsonType.STRING, 0);
        output.WriteString(value);
    }

    protected override void doWriteNull() {
        IDsonOutput output = this._output;
        WriteFullTypeAndCurrentName(output, DsonType.NULL, 0);
    }

    protected override void doWriteBinary(DsonBinary binary) {
        IDsonOutput output = this._output;
        WriteFullTypeAndCurrentName(output, DsonType.BINARY, 0);
        DsonReaderUtils.writeBinary(output, binary);
    }

    protected override void doWriteBinary(int type, DsonChunk chunk) {
        IDsonOutput output = this._output;
        WriteFullTypeAndCurrentName(output, DsonType.BINARY, 0);
        DsonReaderUtils.writeBinary(output, type, chunk);
    }

    protected override void doWriteExtInt32(DsonExtInt32 value, WireType wireType, INumberStyle style) {
        IDsonOutput output = this._output;
        WriteFullTypeAndCurrentName(output, DsonType.EXT_INT32, (int)wireType);
        DsonReaderUtils.writeExtInt32(output, value, wireType);
    }

    protected override void doWriteExtInt64(DsonExtInt64 value, WireType wireType, INumberStyle style) {
        IDsonOutput output = this._output;
        WriteFullTypeAndCurrentName(output, DsonType.EXT_INT64, (int)wireType);
        DsonReaderUtils.writeExtInt64(output, value, wireType);
    }

    protected override void doWriteExtDouble(DsonExtDouble value, INumberStyle style) {
        int wireType = DsonReaderUtils.wireTypeOfDouble(value.Value);
        IDsonOutput output = this._output;
        WriteFullTypeAndCurrentName(output, DsonType.EXT_DOUBLE, wireType);
        DsonReaderUtils.writeExtDouble(output, value, wireType);
    }

    protected override void doWriteExtString(DsonExtString value, StringStyle style) {
        IDsonOutput output = this._output;
        WriteFullTypeAndCurrentName(output, DsonType.EXT_STRING, DsonReaderUtils.wireTypeOfExtString(value));
        DsonReaderUtils.writeExtString(output, value);
    }

    protected override void doWriteRef(ObjectRef objectRef) {
        IDsonOutput output = this._output;
        WriteFullTypeAndCurrentName(output, DsonType.REFERENCE, DsonReaderUtils.wireTypeOfRef(objectRef));
        DsonReaderUtils.writeRef(output, objectRef);
    }

    protected override void doWriteTimestamp(OffsetTimestamp timestamp) {
        IDsonOutput output = this._output;
        WriteFullTypeAndCurrentName(output, DsonType.TIMESTAMP, 0);
        DsonReaderUtils.writeTimestamp(output, timestamp);
    }

    #endregion

    #region 容器

    protected override void doWriteStartContainer(DsonContextType contextType, DsonType dsonType, ObjectStyle style) {
        IDsonOutput output = this._output;
        WriteFullTypeAndCurrentName(output, dsonType, 0);

        Context newContext = NewContext(GetContext(), contextType, dsonType);
        newContext.PreWritten = output.Position;
        output.WriteFixed32(0);

        SetContext(newContext);
        this.RecursionDepth++;
    }

    protected override void doWriteEndContainer() {
        // 记录preWritten在写length之前，最后的size要减4
        Context context = GetContext();
        int preWritten = context.PreWritten;
        _output.SetFixedInt32(preWritten, _output.Position - preWritten - 4);

        this.RecursionDepth--;
        SetContext(context.Parent);
        PoolContext(context);
    }

    #endregion

    #region 特殊

    protected override void doWriteMessage(int binaryType, IMessage messageLite) {
        IDsonOutput output = this._output;
        WriteFullTypeAndCurrentName(output, DsonType.BINARY, 0);
        DsonReaderUtils.writeMessage(output, binaryType, messageLite);
    }

    protected override void doWriteValueBytes(DsonType type, byte[] data) {
        IDsonOutput output = this._output;
        WriteFullTypeAndCurrentName(output, type, 0);
        DsonReaderUtils.writeValueBytes(output, type, data);
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
        context.init(parent, contextType, dsonType);
        return context;
    }

    private void PoolContext(Context context) {
        context.reset();
        SetPooledContext(context);
    }

    protected new class Context : AbstractDsonWriter<TName>.Context
    {
        protected internal int PreWritten = 0;

        public Context() {
        }

        public override void reset() {
            base.reset();
            PreWritten = 0;
        }
    }

    #endregion
}