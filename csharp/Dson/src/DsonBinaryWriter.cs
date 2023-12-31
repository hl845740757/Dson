﻿#region LICENSE

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
using Wjybxx.Dson.Internal;
using Wjybxx.Dson.IO;
using Wjybxx.Dson.Text;
using Wjybxx.Dson.Types;

#pragma warning disable CS1591
namespace Wjybxx.Dson;

/// <summary>
/// Dson二进制Writer
/// </summary>
/// <typeparam name="TName"></typeparam>
public class DsonBinaryWriter<TName> : AbstractDsonWriter<TName> where TName : IEquatable<TName>
{
#nullable disable
    private IDsonOutput _output;
#nullable enable
    private readonly AbstractDsonWriter<string>? _textWriter;
    private readonly AbstractDsonWriter<FieldNumber>? _binWriter;

    public DsonBinaryWriter(DsonWriterSettings settings, IDsonOutput output) : base(settings) {
        this._output = output ?? throw new ArgumentNullException(nameof(output));
        SetContext(new Context().Init(null, DsonContextType.TopLevel, DsonTypes.Invalid));
        if (DsonInternals.IsStringKey<TName>()) {
            _textWriter = this as AbstractDsonWriter<string>;
            _binWriter = null;
        } else {
            _textWriter = null;
            _binWriter = this as AbstractDsonWriter<FieldNumber>;
        }
    }

    protected override Context GetContext() {
        return (Context)_context;
    }

    protected override AbstractDsonWriter<TName>.Context NewContext() {
        return new Context();
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
                    output.WriteString(_textWriter._context.curName);
                } else {
                    output.WriteUint32(_binWriter!._context.curName.FullNumber);
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

    protected override void DoWriteString(string value, StringStyle style) {
        IDsonOutput output = this._output;
        WriteFullTypeAndCurrentName(output, DsonType.String, 0);
        output.WriteString(value);
    }

    protected override void DoWriteNull() {
        IDsonOutput output = this._output;
        WriteFullTypeAndCurrentName(output, DsonType.Null, 0);
    }

    protected override void DoWriteBinary(Binary binary) {
        IDsonOutput output = this._output;
        WriteFullTypeAndCurrentName(output, DsonType.Binary, 0);
        DsonReaderUtils.WriteBinary(output, binary);
    }

    protected override void DoWriteBinary(int type, DsonChunk chunk) {
        IDsonOutput output = this._output;
        WriteFullTypeAndCurrentName(output, DsonType.Binary, 0);
        DsonReaderUtils.WriteBinary(output, type, chunk);
    }

    protected override void DoWriteExtInt32(ExtInt32 extInt32, WireType wireType, INumberStyle style) {
        IDsonOutput output = this._output;
        WriteFullTypeAndCurrentName(output, DsonType.ExtInt32, (int)wireType);
        DsonReaderUtils.WriteExtInt32(output, extInt32, wireType);
    }

    protected override void DoWriteExtInt64(ExtInt64 extInt64, WireType wireType, INumberStyle style) {
        IDsonOutput output = this._output;
        WriteFullTypeAndCurrentName(output, DsonType.ExtInt64, (int)wireType);
        DsonReaderUtils.WriteExtInt64(output, extInt64, wireType);
    }

    protected override void DoWriteExtDouble(ExtDouble extDouble, INumberStyle style) {
        int wireType = DsonReaderUtils.WireTypeOfDouble(extDouble.Value);
        IDsonOutput output = this._output;
        WriteFullTypeAndCurrentName(output, DsonType.ExtDouble, wireType);
        DsonReaderUtils.WriteExtDouble(output, extDouble, wireType);
    }

    protected override void DoWriteExtString(ExtString extString, StringStyle style) {
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
        newContext.preWritten = output.Position;
        output.WriteFixed32(0);

        SetContext(newContext);
        this._recursionDepth++;
    }

    protected override void DoWriteEndContainer() {
        // 记录preWritten在写length之前，最后的size要减4
        Context context = GetContext();
        int preWritten = context.preWritten;
        _output.SetFixedInt32(preWritten, _output.Position - preWritten - 4);

        this._recursionDepth--;
        SetContext(context.Parent);
        ReturnContext(context);
    }

    #endregion

    #region 特殊

    protected override void DoWriteValueBytes(DsonType type, byte[] data) {
        IDsonOutput output = this._output;
        WriteFullTypeAndCurrentName(output, type, 0);
        DsonReaderUtils.WriteValueBytes(output, type, data);
    }

    #endregion

    #region context

    private Context NewContext(Context parent, DsonContextType contextType, DsonType dsonType) {
        Context context = (Context)RentContext();
        context.Init(parent, contextType, dsonType);
        return context;
    }

    protected new class Context : AbstractDsonWriter<TName>.Context
    {
        protected internal int preWritten;

        public Context() {
        }

        public override void Reset() {
            base.Reset();
            preWritten = 0;
        }
    }

    #endregion
}