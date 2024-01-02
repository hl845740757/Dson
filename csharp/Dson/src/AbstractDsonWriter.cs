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
using System.Collections.Generic;
using Wjybxx.Dson.IO;
using Wjybxx.Dson.Text;
using Wjybxx.Dson.Types;

#pragma warning disable CS1591
namespace Wjybxx.Dson;

public abstract class AbstractDsonWriter<TName> : IDsonWriter<TName> where TName : IEquatable<TName>
{
    protected readonly DsonWriterSettings Settings;
    private const int PoolSize = 4;

#nullable disable
    internal Context _context;
    private Stack<Context> _contextPool = new(PoolSize);
    protected int _recursionDepth = 0; // 当前递归深度
#nullable enable

    protected AbstractDsonWriter(DsonWriterSettings settings) {
        this.Settings = settings ?? throw new ArgumentNullException(nameof(settings));
    }

    public DsonWriterSettings WriterSettings => Settings;

    protected virtual Context GetContext() {
        return _context;
    }

    protected void SetContext(Context context) {
        this._context = context;
    }

    protected abstract Context NewContext();

    protected Context RentContext() {
        if (_contextPool.TryPop(out Context? context)) {
            return context;
        }
        return NewContext();
    }

    protected void ReturnContext(Context context) {
        context.Reset();
        if (_contextPool.Count < PoolSize) {
            _contextPool.Push(context);
        }
    }

    public abstract void Flush();

    public virtual void Dispose() {
        _context = null!;
        _contextPool.Clear();
        _contextPool = null!;
    }

    #region state

    public DsonContextType ContextType => _context.contextType;

    public TName CurrentName {
        get {
            Context context = this._context;
            if (context.state != DsonWriterState.Value) {
                throw InvalidState(DsonInternals.NewList(DsonWriterState.Value), context.state);
            }
            return context.curName;
        }
    }

    public bool IsAtName => _context.state == DsonWriterState.Name;

    public void WriteName(TName name) {
        if (name == null) throw new ArgumentNullException(nameof(name));
        Context context = this._context;
        if (context.state != DsonWriterState.Name) {
            throw InvalidState(DsonInternals.NewList(DsonWriterState.Name), context.state);
        }
        context.curName = name;
        context.state = DsonWriterState.Value;
        DoWriteName(name);
    }

    /** 执行{@link #WriteName(String)}时调用 */
    protected void DoWriteName(TName name) {
    }

    protected void AdvanceToValueState(TName name) {
        Context context = this._context;
        if (context.state == DsonWriterState.Name) {
            WriteName(name);
        }
        if (context.state != DsonWriterState.Value) {
            throw InvalidState(DsonInternals.NewList(DsonWriterState.Value), context.state);
        }
    }

    protected void EnsureValueState(Context context) {
        if (context.state != DsonWriterState.Value) {
            throw InvalidState(DsonInternals.NewList(DsonWriterState.Value), context.state);
        }
    }

    protected void SetNextState() {
        switch (_context.contextType) {
            case DsonContextType.Object:
            case DsonContextType.Header: {
                _context.SetState(DsonWriterState.Name);
                break;
            }
            case DsonContextType.TopLevel:
            case DsonContextType.Array: {
                _context.SetState(DsonWriterState.Value);
                break;
            }
            default: throw new InvalidOperationException();
        }
    }

    private DsonIOException InvalidState(List<DsonWriterState> expected, DsonWriterState state) {
        return DsonIOException.InvalidState(_context.contextType, expected, state);
    }

    #endregion

    #region 简单值

    public void WriteInt32(TName name, int value, WireType wireType, INumberStyle? style = null) {
        AdvanceToValueState(name);
        DoWriteInt32(value, wireType, style ?? NumberStyles.Simple);
        SetNextState();
    }

    public void WriteInt64(TName name, long value, WireType wireType, INumberStyle? style = null) {
        AdvanceToValueState(name);
        DoWriteInt64(value, wireType, style ?? NumberStyles.Simple);
        SetNextState();
    }

    public void WriteFloat(TName name, float value, INumberStyle? style = null) {
        AdvanceToValueState(name);
        DoWriteFloat(value, style ?? NumberStyles.Simple);
        SetNextState();
    }

    public void WriteDouble(TName name, double value, INumberStyle? style = null) {
        AdvanceToValueState(name);
        DoWriteDouble(value, style ?? NumberStyles.Simple);
        SetNextState();
    }

    public void WriteBoolean(TName name, bool value) {
        AdvanceToValueState(name);
        DoWriteBool(value);
        SetNextState();
    }

    public void WriteString(TName name, string value, StringStyle style = StringStyle.Auto) {
        if (value == null) throw new ArgumentNullException(nameof(value));
        AdvanceToValueState(name);
        DoWriteString(value, style);
        SetNextState();
    }

    public void WriteNull(TName name) {
        AdvanceToValueState(name);
        DoWriteNull();
        SetNextState();
    }

    public void WriteBinary(TName name, Binary binary) {
        AdvanceToValueState(name);
        DoWriteBinary(binary);
        SetNextState();
    }

    public void WriteBinary(TName name, int type, DsonChunk chunk) {
        Dsons.CheckSubType(type);
        Dsons.CheckBinaryLength(chunk.Length);
        AdvanceToValueState(name);
        DoWriteBinary(type, chunk);
        SetNextState();
    }

    public void WriteExtInt32(TName name, ExtInt32 extInt32, WireType wireType, INumberStyle? style = null) {
        AdvanceToValueState(name);
        DoWriteExtInt32(extInt32, wireType, style ?? NumberStyles.Simple);
        SetNextState();
    }

    public void WriteExtInt64(TName name, ExtInt64 extInt64, WireType wireType, INumberStyle? style = null) {
        AdvanceToValueState(name);
        DoWriteExtInt64(extInt64, wireType, style ?? NumberStyles.Simple);
        SetNextState();
    }

    public void WriteExtDouble(TName name, ExtDouble extDouble, INumberStyle? style = null) {
        AdvanceToValueState(name);
        DoWriteExtDouble(extDouble, style ?? NumberStyles.Simple);
        SetNextState();
    }

    public void WriteExtString(TName name, ExtString extString, StringStyle style = StringStyle.Auto) {
        AdvanceToValueState(name);
        DoWriteExtString(extString, style);
        SetNextState();
    }

    public void WriteRef(TName name, ObjectRef objectRef) {
        AdvanceToValueState(name);
        DoWriteRef(objectRef);
        SetNextState();
    }

    public void WriteTimestamp(TName name, OffsetTimestamp timestamp) {
        AdvanceToValueState(name);
        DoWriteTimestamp(timestamp);
        SetNextState();
    }

    protected abstract void DoWriteInt32(int value, WireType wireType, INumberStyle style);

    protected abstract void DoWriteInt64(long value, WireType wireType, INumberStyle style);

    protected abstract void DoWriteFloat(float value, INumberStyle style);

    protected abstract void DoWriteDouble(double value, INumberStyle style);

    protected abstract void DoWriteBool(bool value);

    protected abstract void DoWriteString(string value, StringStyle style);

    protected abstract void DoWriteNull();

    protected abstract void DoWriteBinary(Binary binary);

    protected abstract void DoWriteBinary(int type, DsonChunk chunk);

    protected abstract void DoWriteExtInt32(ExtInt32 extInt32, WireType wireType, INumberStyle style);

    protected abstract void DoWriteExtInt64(ExtInt64 extInt64, WireType wireType, INumberStyle style);

    protected abstract void DoWriteExtDouble(ExtDouble extDouble, INumberStyle style);

    protected abstract void DoWriteExtString(ExtString extString, StringStyle style);

    protected abstract void DoWriteRef(ObjectRef objectRef);

    protected abstract void DoWriteTimestamp(OffsetTimestamp timestamp);

    #endregion

    #region 容器

    public void WriteStartArray(ObjectStyle style) {
        WriteStartContainer(DsonContextType.Array, DsonType.Array, style);
    }

    public void WriteEndArray() {
        WriteEndContainer(DsonContextType.Array, DsonWriterState.Value);
    }

    public void WriteStartObject(ObjectStyle style) {
        WriteStartContainer(DsonContextType.Object, DsonType.Object, style);
    }

    public void WriteEndObject() {
        WriteEndContainer(DsonContextType.Object, DsonWriterState.Name);
    }

    public void WriteStartHeader(ObjectStyle style) {
        // object下默认是name状态
        Context context = this._context;
        if (context.contextType == DsonContextType.Object && context.state == DsonWriterState.Name) {
            context.SetState(DsonWriterState.Value);
        }
        WriteStartContainer(DsonContextType.Header, DsonType.Header, style);
    }

    public void WriteEndHeader() {
        WriteEndContainer(DsonContextType.Header, DsonWriterState.Name);
    }

    private void WriteStartContainer(DsonContextType contextType, DsonType dsonType, ObjectStyle style) {
        if (_recursionDepth >= Settings.RecursionLimit) {
            throw DsonIOException.RecursionLimitExceeded();
        }
        Context context = this._context;
        AutoStartTopLevel(context);
        EnsureValueState(context);
        DoWriteStartContainer(contextType, dsonType, style);
        SetNextState(); // 设置新上下文状态
    }

    private void WriteEndContainer(DsonContextType contextType, DsonWriterState expectedState) {
        Context context = this._context;
        CheckEndContext(context, contextType, expectedState);
        DoWriteEndContainer();
        SetNextState(); // parent前进一个状态
    }

    protected void AutoStartTopLevel(Context context) {
        if (context.contextType == DsonContextType.TopLevel
            && context.state == DsonWriterState.Initial) {
            context.SetState(DsonWriterState.Value);
        }
    }

    protected void CheckEndContext(Context context, DsonContextType contextType, DsonWriterState state) {
        if (context.contextType != contextType) {
            throw DsonIOException.ContextError(contextType, context.contextType);
        }
        if (context.state != state) {
            throw InvalidState(DsonInternals.NewList(state), context.state);
        }
    }

    /** 写入类型信息，创建新上下文，压入上下文 */
    protected abstract void DoWriteStartContainer(DsonContextType contextType, DsonType dsonType, ObjectStyle style);

    /** 弹出上下文 */
    protected abstract void DoWriteEndContainer();

    #endregion

    #region 特殊

    public virtual void WriteSimpleHeader(string clsName) {
        if (clsName == null) throw new ArgumentNullException(nameof(clsName));
        IDsonWriter<string> textWrite = (IDsonWriter<string>)this;
        textWrite.WriteStartHeader();
        textWrite.WriteString(DsonHeaders.NamesClassName, clsName, StringStyle.AutoQuote);
        textWrite.WriteEndHeader();
    }

    public void WriteValueBytes(TName name, DsonType type, byte[] data) {
        DsonReaderUtils.CheckWriteValueAsBytes(type);
        AdvanceToValueState(name);
        DoWriteValueBytes(type, data);
        SetNextState();
    }

    public object? Attach(object userData) {
        return _context.Attach(userData);
    }

    public object Attachment() {
        return _context.userData;
    }

    protected abstract void DoWriteValueBytes(DsonType type, byte[] data);

    #endregion

    #region contxt

#nullable enable
    protected internal abstract class Context
    {
#nullable disable
        protected internal Context parent;
        protected internal DsonContextType contextType;
        protected internal DsonType dsonType; // 用于在Object/Array模式下写入内置数据结构
        protected internal DsonWriterState state = DsonWriterState.Initial;
        protected internal TName curName;
        protected internal object userData;
#nullable enable

        public Context() {
        }

        public Context Init(Context? parent, DsonContextType contextType, DsonType dsonType) {
            this.parent = parent;
            this.contextType = contextType;
            this.dsonType = dsonType;
            return this;
        }

        public virtual void Reset() {
            parent = null;
            contextType = default;
            dsonType = DsonTypes.Invalid;
            state = default;
            curName = default;
            userData = null;
        }

        public object? Attach(object? userData) {
            object? r = this.userData;
            this.userData = userData;
            return r;
        }

        /** 方便查看赋值的调用 */
        public void SetState(DsonWriterState state) {
            this.state = state;
        }

        /** 子类可隐藏 */
        public Context Parent => parent;
    }

    #endregion
}