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

public abstract class AbstractDsonWriter<TName> : IDsonWriter<TName> where TName : IEquatable<TName>
{
    protected readonly DsonWriterSettings Settings;

#nullable disable
    internal Context _context;
    private Context _pooledContext; // 一个额外的缓存，用于写集合等减少上下文创建
    protected int _recursionDepth = 0; // 当前递归深度
#nullable enable

    protected AbstractDsonWriter(DsonWriterSettings settings) {
        this.Settings = settings;
    }

    public DsonWriterSettings WriterSettings => Settings;

    protected virtual Context GetContext() {
        return _context;
    }

    protected void SetContext(Context context) {
        this._context = context;
    }

    protected virtual Context? GetPooledContext() {
        return _pooledContext;
    }

    protected void SetPooledContext(Context? pooledContext) {
        this._pooledContext = pooledContext;
    }

    public abstract void Flush();

    public virtual void Dispose() {
        _context = null!;
        _pooledContext = null;
    }

    #region state

    public DsonContextType ContextType => _context._contextType;

    public bool IsAtName => _context._state == DsonWriterState.Name;

    public void WriteName(TName name) {
        if (name == null) throw new ArgumentNullException(nameof(name));
        Context context = this._context;
        if (context._state != DsonWriterState.Name) {
            throw InvalidState(DsonInternals.NewList(DsonWriterState.Name), context._state);
        }
        context._curName = name;
        context._state = DsonWriterState.Value;
        DoWriteName(name);
    }

    /** 执行{@link #WriteName(String)}时调用 */
    protected void DoWriteName(TName name) {
    }

    protected void AdvanceToValueState(TName name) {
        Context context = this._context;
        if (context._state == DsonWriterState.Name) {
            WriteName(name);
        }
        if (context._state != DsonWriterState.Value) {
            throw InvalidState(DsonInternals.NewList(DsonWriterState.Value), context._state);
        }
    }

    protected void EnsureValueState(Context context) {
        if (context._state != DsonWriterState.Value) {
            throw InvalidState(DsonInternals.NewList(DsonWriterState.Value), context._state);
        }
    }

    protected void SetNextState() {
        switch (_context._contextType) {
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
        return DsonIOException.invalidState(_context._contextType, expected, state);
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

    public void WriteBinary(TName name, DsonBinary dsonBinary) {
        if (dsonBinary == null) throw new ArgumentNullException(nameof(dsonBinary));
        AdvanceToValueState(name);
        DoWriteBinary(dsonBinary);
        SetNextState();
    }

    public void WriteBinary(TName name, int type, DsonChunk chunk) {
        if (chunk == null) throw new ArgumentNullException(nameof(chunk));
        Dsons.CheckSubType(type);
        Dsons.CheckBinaryLength(chunk.Length);
        AdvanceToValueState(name);
        DoWriteBinary(type, chunk);
        SetNextState();
    }

    public void WriteExtInt32(TName name, DsonExtInt32 value, WireType wireType, INumberStyle? style = null) {
        if (value == null) throw new ArgumentNullException(nameof(value));
        AdvanceToValueState(name);
        DoWriteExtInt32(value, wireType, style ?? NumberStyles.Simple);
        SetNextState();
    }

    public void WriteExtInt64(TName name, DsonExtInt64 value, WireType wireType, INumberStyle? style = null) {
        if (value == null) throw new ArgumentNullException(nameof(value));
        AdvanceToValueState(name);
        DoWriteExtInt64(value, wireType, style ?? NumberStyles.Simple);
        SetNextState();
    }

    public void WriteExtDouble(TName name, DsonExtDouble value, INumberStyle? style = null) {
        if (value == null) throw new ArgumentNullException(nameof(value));
        AdvanceToValueState(name);
        DoWriteExtDouble(value, style ?? NumberStyles.Simple);
        SetNextState();
    }

    public void WriteExtString(TName name, DsonExtString value, StringStyle style = StringStyle.Auto) {
        if (value == null) throw new ArgumentNullException(nameof(value));
        AdvanceToValueState(name);
        DoWriteExtString(value, style);
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

    protected abstract void DoWriteString(String value, StringStyle style);

    protected abstract void DoWriteNull();

    protected abstract void DoWriteBinary(DsonBinary binary);

    protected abstract void DoWriteBinary(int type, DsonChunk chunk);

    protected abstract void DoWriteExtInt32(DsonExtInt32 extInt32, WireType wireType, INumberStyle style);

    protected abstract void DoWriteExtInt64(DsonExtInt64 extInt64, WireType wireType, INumberStyle style);

    protected abstract void DoWriteExtDouble(DsonExtDouble extDouble, INumberStyle style);

    protected abstract void DoWriteExtString(DsonExtString extString, StringStyle style);

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
        if (context._contextType == DsonContextType.Object && context._state == DsonWriterState.Name) {
            context.SetState(DsonWriterState.Value);
        }
        WriteStartContainer(DsonContextType.Header, DsonType.Header, style);
    }

    public void WriteEndHeader() {
        WriteEndContainer(DsonContextType.Header, DsonWriterState.Name);
    }

    private void WriteStartContainer(DsonContextType contextType, DsonType dsonType, ObjectStyle style) {
        if (_recursionDepth >= Settings.RecursionLimit) {
            throw DsonIOException.recursionLimitExceeded();
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
        if (context._contextType == DsonContextType.TopLevel
            && context._state == DsonWriterState.Initial) {
            context.SetState(DsonWriterState.Value);
        }
    }

    protected void CheckEndContext(Context context, DsonContextType contextType, DsonWriterState state) {
        if (context._contextType != contextType) {
            throw DsonIOException.contextError(contextType, context._contextType);
        }
        if (context._state != state) {
            throw InvalidState(DsonInternals.NewList(state), context._state);
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

    public void WriteMessage(TName name, int binaryType, IMessage message) {
        Dsons.CheckSubType(binaryType);
        AdvanceToValueState(name);
        DoWriteMessage(binaryType, message);
        SetNextState();
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
        return _context._userData;
    }

    protected abstract void DoWriteMessage(int binaryType, IMessage message);

    protected abstract void DoWriteValueBytes(DsonType type, byte[] data);

    #endregion

    #region contxt

#nullable enable
    protected internal class Context
    {
#nullable disable
        protected internal Context _parent;
        protected internal DsonContextType _contextType;
        protected internal DsonType _dsonType; // 用于在Object/Array模式下写入内置数据结构
        protected internal DsonWriterState _state = DsonWriterState.Initial;
        protected internal TName _curName;
        protected internal object _userData;
#nullable enable

        public Context() {
        }

        public Context Init(Context? parent, DsonContextType contextType, DsonType dsonType) {
            this._parent = parent;
            this._contextType = contextType;
            this._dsonType = dsonType;
            return this;
        }

        public virtual void Reset() {
            _parent = null;
            _contextType = default;
            _dsonType = DsonTypes.Invalid;
            _state = default;
            _curName = default;
            _userData = null;
        }

        public object? Attach(object? userData) {
            object? r = this._userData;
            this._userData = userData;
            return r;
        }

        /** 方便查看赋值的调用 */
        public void SetState(DsonWriterState state) {
            this._state = state;
        }

        /** 子类可隐藏 */
        public Context Parent => _parent;
    }

    #endregion
}