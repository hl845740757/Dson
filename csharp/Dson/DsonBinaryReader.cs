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
using Dson.Types;
using Google.Protobuf;

namespace Dson;

public class DsonBinaryReader<TName> : AbstractDsonReader<TName> where TName : IEquatable<TName>
{
    private IDsonInput _input;
    private readonly AbstractDsonReader<string>? _textReader;
    private readonly AbstractDsonReader<FieldNumber>? _binReader;

    public DsonBinaryReader(DsonReaderSettings settings, IDsonInput input) : base(settings) {
        this._input = input;
        if (DsonInternals.IsStringKey<TName>()) {
            this._textReader = this as AbstractDsonReader<string>;
            this._binReader = null;
        }
        else {
            this._textReader = null;
            this._binReader = this as AbstractDsonReader<FieldNumber>;
        }
    }

    protected override Context GetContext() {
        return (Context)_context;
    }

    protected override Context? GetPooledContext() {
        return (Context)base.GetPooledContext();
    }

    public override void Dispose() {
        if (Settings.autoClose) {
            _input?.Dispose();
            _input = null!;
        }
        base.Dispose();
    }

    #region state

    public override DsonType ReadDsonType() {
        Context context = GetContext();
        checkReadDsonTypeState(context);

        int fullType = _input.IsAtEnd() ? 0 : BinaryUtils.ToUint(_input.ReadRawByte());
        int wreTypeBits = Dsons.WireTypeOfFullType(fullType);
        DsonType dsonType = DsonTypes.ForNumber(Dsons.DsonTypeOfFullType(fullType));
        WireType wireType = dsonType.HasWireType() ? WireTypes.ForNumber(wreTypeBits) : WireType.VarInt;
        this.currentDsonType = dsonType;
        this.currentWireType = wireType;
        this.currentWireTypeBits = wreTypeBits;
        this.currentName = default;

        onReadDsonType(context, dsonType);
        return dsonType;
    }

    public override DsonType PeekDsonType() {
        Context context = GetContext();
        checkReadDsonTypeState(context);

        int fullType = _input.IsAtEnd() ? 0 : BinaryUtils.ToUint(_input.GetByte(_input.Position));
        return DsonTypes.ForNumber(Dsons.DsonTypeOfFullType(fullType));
    }

    protected override void doReadName() {
        if (_textReader != null) {
            string filedName = _input.ReadString();
            if (Settings.enableFieldIntern) {
                filedName = Dsons.InternField(filedName);
            }
            _textReader.currentName = filedName;
        }
        else {
            _binReader!.currentName = FieldNumber.OfFullNumber(_input.ReadUint32());
        }
    }

    #endregion

    #region 简单值

    protected override int doReadInt32() {
        return DsonReaderUtils.readInt32(_input, currentWireType);
    }

    protected override long doReadInt64() {
        return DsonReaderUtils.readInt64(_input, currentWireType);
    }

    protected override float doReadFloat() {
        return DsonReaderUtils.readFloat(_input, currentWireTypeBits);
    }

    protected override double doReadDouble() {
        return DsonReaderUtils.readDouble(_input, currentWireTypeBits);
    }

    protected override bool doReadBool() {
        return DsonReaderUtils.readBool(_input, currentWireTypeBits);
    }

    protected override String doReadString() {
        return _input.ReadString();
    }

    protected override void doReadNull() {
    }

    protected override DsonBinary doReadBinary() {
        return DsonReaderUtils.readDsonBinary(_input);
    }

    protected override DsonExtInt32 doReadExtInt32() {
        return DsonReaderUtils.readDsonExtInt32(_input, currentWireType);
    }

    protected override DsonExtInt64 doReadExtInt64() {
        return DsonReaderUtils.readDsonExtInt64(_input, currentWireType);
    }

    protected override DsonExtDouble doReadExtDouble() {
        return DsonReaderUtils.readDsonExtDouble(_input, currentWireTypeBits);
    }

    protected override DsonExtString doReadExtString() {
        return DsonReaderUtils.readDsonExtString(_input, currentWireTypeBits);
    }

    protected override ObjectRef doReadRef() {
        return DsonReaderUtils.readRef(_input, currentWireTypeBits);
    }

    protected override OffsetTimestamp doReadTimestamp() {
        return DsonReaderUtils.readTimestamp(_input);
    }

    #endregion

    #region 容器

    protected override void doReadStartContainer(DsonContextType contextType, DsonType dsonType) {
        Context newContext = NewContext(GetContext(), contextType, dsonType);
        int length = _input.ReadFixed32();
        newContext.OldLimit = _input.PushLimit(length);
        newContext.name = currentName;

        this.recursionDepth++;
        SetContext(newContext);
    }

    protected override void doReadEndContainer() {
        if (!_input.IsAtEnd()) {
            throw DsonIOException.bytesRemain(_input.GetBytesUntilLimit());
        }
        Context context = GetContext();
        _input.PopLimit(context.OldLimit);

        // 恢复上下文
        recoverDsonType(context);
        this.recursionDepth--;
        SetContext(context.parent!);
        PoolContext(context);
    }

    #endregion

    #region 特殊

    protected override void doSkipName() {
        if (_textReader != null) {
            // 避免构建字符串
            int size = _input.ReadUint32();
            if (size > 0) {
                _input.SkipRawBytes(size);
            }
        }
        else {
            _input.ReadUint32();
        }
    }

    protected override void doSkipValue() {
        DsonReaderUtils.skipValue(_input, ContextType, currentDsonType, currentWireType, currentWireTypeBits);
    }

    protected override void doSkipToEndOfObject() {
        DsonReaderUtils.skipToEndOfObject(_input);
    }

    protected override T doReadMessage<T>(int binaryType, MessageParser<T> parser) {
        return DsonReaderUtils.readMessage(_input, binaryType, parser);
    }

    protected override byte[] doReadValueAsBytes() {
        return DsonReaderUtils.readValueAsBytes(_input, currentDsonType);
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

    protected new class Context : AbstractDsonReader<TName>.Context
    {
        protected internal int OldLimit = -1;

        public Context() {
        }

        public override void reset() {
            base.reset();
            OldLimit = -1;
        }
    }

    #endregion
}