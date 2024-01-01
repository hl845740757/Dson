#region LICENSE

//  Copyright 2023 wjybxx(845740757@qq.com)
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
using Wjybxx.Dson.IO;
using Wjybxx.Dson.Types;

#pragma warning disable CS1591
namespace Wjybxx.Dson;

/// <summary>
/// Dson二进制Reader
/// </summary>
/// <typeparam name="TName"></typeparam>
public class DsonBinaryReader<TName> : AbstractDsonReader<TName> where TName : IEquatable<TName>
{
#nullable disable
    private IDsonInput _input;
#nullable enable
    private readonly AbstractDsonReader<string>? _textReader;
    private readonly AbstractDsonReader<FieldNumber>? _binReader;

    public DsonBinaryReader(DsonReaderSettings settings, IDsonInput input) : base(settings) {
        this._input = input ?? throw new ArgumentNullException(nameof(input));
        SetContext(new Context().Init(null, DsonContextType.TopLevel, DsonTypes.Invalid));
        if (DsonInternals.IsStringKey<TName>()) {
            this._textReader = this as AbstractDsonReader<string>;
            this._binReader = null;
        } else {
            this._textReader = null;
            this._binReader = this as AbstractDsonReader<FieldNumber>;
        }
    }

    protected override Context GetContext() {
        return (Context)_context;
    }

    protected override Context? GetPooledContext() {
        return (Context?)base.GetPooledContext();
    }

    public override void Dispose() {
        if (Settings.AutoClose) {
            _input?.Dispose();
            _input = null!;
        }
        base.Dispose();
    }

    #region state

    public override DsonType ReadDsonType() {
        Context context = GetContext();
        CheckReadDsonTypeState(context);

        int fullType = _input.IsAtEnd() ? 0 : BinaryUtils.ToUint(_input.ReadRawByte());
        int wreTypeBits = Dsons.WireTypeOfFullType(fullType);
        DsonType dsonType = DsonTypes.ForNumber(Dsons.DsonTypeOfFullType(fullType));
        WireType wireType = dsonType.HasWireType() ? WireTypes.ForNumber(wreTypeBits) : WireType.VarInt;
        this._currentDsonType = dsonType;
        this._currentWireType = wireType;
        this._currentWireTypeBits = wreTypeBits;
        this._currentName = default!;

        OnReadDsonType(context, dsonType);
        return dsonType;
    }

    public override DsonType PeekDsonType() {
        Context context = GetContext();
        CheckReadDsonTypeState(context);

        int fullType = _input.IsAtEnd() ? 0 : BinaryUtils.ToUint(_input.GetByte(_input.Position));
        return DsonTypes.ForNumber(Dsons.DsonTypeOfFullType(fullType));
    }

    protected override void DoReadName() {
        if (_textReader != null) {
            string filedName = _input.ReadString();
            if (Settings.EnableFieldIntern) {
                filedName = Dsons.InternField(filedName);
            }
            _textReader._currentName = filedName;
        } else {
            _binReader!._currentName = FieldNumber.OfFullNumber(_input.ReadUint32());
        }
    }

    #endregion

    #region 简单值

    protected override int DoReadInt32() {
        return DsonReaderUtils.ReadInt32(_input, _currentWireType);
    }

    protected override long DoReadInt64() {
        return DsonReaderUtils.ReadInt64(_input, _currentWireType);
    }

    protected override float DoReadFloat() {
        return DsonReaderUtils.ReadFloat(_input, _currentWireTypeBits);
    }

    protected override double DoReadDouble() {
        return DsonReaderUtils.ReadDouble(_input, _currentWireTypeBits);
    }

    protected override bool DoReadBool() {
        return DsonReaderUtils.ReadBool(_input, _currentWireTypeBits);
    }

    protected override string DoReadString() {
        return _input.ReadString();
    }

    protected override void DoReadNull() {
    }

    protected override Binary DoReadBinary() {
        return DsonReaderUtils.ReadBinary(_input);
    }

    protected override ExtInt32 DoReadExtInt32() {
        return DsonReaderUtils.ReadExtInt32(_input, _currentWireType);
    }

    protected override ExtInt64 DoReadExtInt64() {
        return DsonReaderUtils.ReadExtInt64(_input, _currentWireType);
    }

    protected override ExtDouble DoReadExtDouble() {
        return DsonReaderUtils.ReadExtDouble(_input, _currentWireTypeBits);
    }

    protected override ExtString DoReadExtString() {
        return DsonReaderUtils.ReadExtString(_input, _currentWireTypeBits);
    }

    protected override ObjectRef DoReadRef() {
        return DsonReaderUtils.ReadRef(_input, _currentWireTypeBits);
    }

    protected override OffsetTimestamp DoReadTimestamp() {
        return DsonReaderUtils.ReadTimestamp(_input);
    }

    #endregion

    #region 容器

    protected override void DoReadStartContainer(DsonContextType contextType, DsonType dsonType) {
        Context newContext = NewContext(GetContext(), contextType, dsonType);
        int length = _input.ReadFixed32();
        newContext.oldLimit = _input.PushLimit(length);
        newContext.name = _currentName;

        this._recursionDepth++;
        SetContext(newContext);
    }

    protected override void DoReadEndContainer() {
        if (!_input.IsAtEnd()) {
            throw DsonIOException.BytesRemain(_input.GetBytesUntilLimit());
        }
        Context context = GetContext();
        _input.PopLimit(context.oldLimit);

        // 恢复上下文
        RecoverDsonType(context);
        this._recursionDepth--;
        SetContext(context.parent!);
        PoolContext(context);
    }

    #endregion

    #region 特殊

    protected override void DoSkipName() {
        if (_textReader != null) {
            // 避免构建字符串
            int size = _input.ReadUint32();
            if (size > 0) {
                _input.SkipRawBytes(size);
            }
        } else {
            _input.ReadUint32();
        }
    }

    protected override void DoSkipValue() {
        DsonReaderUtils.SkipValue(_input, ContextType, _currentDsonType, _currentWireType, _currentWireTypeBits);
    }

    protected override void DoSkipToEndOfObject() {
        DsonReaderUtils.SkipToEndOfObject(_input);
    }

    protected override byte[] DoReadValueAsBytes() {
        return DsonReaderUtils.ReadValueAsBytes(_input, _currentDsonType);
    }

    #endregion

    #region context

    private Context NewContext(Context parent, DsonContextType contextType, DsonType dsonType) {
        Context? context = GetPooledContext();
        if (context != null) {
            SetPooledContext(null);
        } else {
            context = new Context();
        }
        context.Init(parent, contextType, dsonType);
        return context;
    }

    private void PoolContext(Context context) {
        context.Reset();
        SetPooledContext(context);
    }

    protected new class Context : AbstractDsonReader<TName>.Context
    {
        protected internal int oldLimit = -1;

        public Context() {
        }

        public override void Reset() {
            base.Reset();
            oldLimit = -1;
        }
    }

    #endregion
}