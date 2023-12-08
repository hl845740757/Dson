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

public abstract class AbstractDsonWriter<TName> : IDsonWriter<TName> where TName : IEquatable<TName>
{
    protected readonly DsonWriterSettings Settings;

#nullable disable
    internal Context _context;
    private Context _pooledContext; // 一个额外的缓存，用于写集合等减少上下文创建
    protected int RecursionDepth = 0; // 当前递归深度
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

    public DsonContextType ContextType => _context.contextType;

    public bool IsAtName => _context.state == DsonWriterState.NAME;

    public void WriteName(TName name) {
        if (name == null) throw new ArgumentNullException(nameof(name));
        Context context = this._context;
        if (context.state != DsonWriterState.NAME) {
            throw invalidState(DsonInternals.NewList(DsonWriterState.NAME), context.state);
        }
        context.curName = name;
        context.state = DsonWriterState.VALUE;
        doWriteName(name);
    }

    /** 执行{@link #WriteName(String)}时调用 */
    protected void doWriteName(TName name) {
    }

    protected void advanceToValueState(TName name) {
        Context context = this._context;
        if (context.state == DsonWriterState.NAME) {
            WriteName(name);
        }
        if (context.state != DsonWriterState.VALUE) {
            throw invalidState(DsonInternals.NewList(DsonWriterState.VALUE), context.state);
        }
    }

    protected void ensureValueState(Context context) {
        if (context.state != DsonWriterState.VALUE) {
            throw invalidState(DsonInternals.NewList(DsonWriterState.VALUE), context.state);
        }
    }

    protected void setNextState() {
        switch (_context.contextType) {
            case DsonContextType.OBJECT:
            case DsonContextType.HEADER: {
                _context.setState(DsonWriterState.NAME);
                break;
            }
            case DsonContextType.TOP_LEVEL:
            case DsonContextType.ARRAY: {
                _context.setState(DsonWriterState.VALUE);
                break;
            }
            default: throw new InvalidOperationException();
        }
    }

    private DsonIOException invalidState(List<DsonWriterState> expected, DsonWriterState state) {
        return DsonIOException.invalidState(_context.contextType, expected, state);
    }

    #endregion

    #region 简单值

    public void WriteInt32(TName name, int value, WireType wireType, INumberStyle? style = null) {
        advanceToValueState(name);
        doWriteInt32(value, wireType, style ?? NumberStyles.Simple);
        setNextState();
    }

    public void WriteInt64(TName name, long value, WireType wireType, INumberStyle? style = null) {
        advanceToValueState(name);
        doWriteInt64(value, wireType, style ?? NumberStyles.Simple);
        setNextState();
    }

    public void WriteFloat(TName name, float value, INumberStyle? style = null) {
        advanceToValueState(name);
        doWriteFloat(value, style ?? NumberStyles.Simple);
        setNextState();
    }

    public void WriteDouble(TName name, double value, INumberStyle? style = null) {
        advanceToValueState(name);
        doWriteDouble(value, style ?? NumberStyles.Simple);
        setNextState();
    }

    public void WriteBoolean(TName name, bool value) {
        advanceToValueState(name);
        doWriteBool(value);
        setNextState();
    }

    public void WriteString(TName name, string value, StringStyle style = StringStyle.Auto) {
        if (value == null) throw new ArgumentNullException(nameof(value));
        advanceToValueState(name);
        doWriteString(value, style);
        setNextState();
    }

    public void WriteNull(TName name) {
        advanceToValueState(name);
        doWriteNull();
        setNextState();
    }

    public void WriteBinary(TName name, DsonBinary dsonBinary) {
        if (dsonBinary == null) throw new ArgumentNullException(nameof(dsonBinary));
        advanceToValueState(name);
        doWriteBinary(dsonBinary);
        setNextState();
    }

    public void WriteBinary(TName name, int type, DsonChunk chunk) {
        if (chunk == null) throw new ArgumentNullException(nameof(chunk));
        Dsons.CheckSubType(type);
        Dsons.CheckBinaryLength(chunk.Length);
        advanceToValueState(name);
        doWriteBinary(type, chunk);
        setNextState();
    }

    public void WriteExtInt32(TName name, DsonExtInt32 value, WireType wireType, INumberStyle? style = null) {
        if (value == null) throw new ArgumentNullException(nameof(value));
        advanceToValueState(name);
        doWriteExtInt32(value, wireType, style ?? NumberStyles.Simple);
        setNextState();
    }

    public void WriteExtInt64(TName name, DsonExtInt64 value, WireType wireType, INumberStyle? style = null) {
        if (value == null) throw new ArgumentNullException(nameof(value));
        advanceToValueState(name);
        doWriteExtInt64(value, wireType, style ?? NumberStyles.Simple);
        setNextState();
    }

    public void WriteExtDouble(TName name, DsonExtDouble value, INumberStyle? style = null) {
        if (value == null) throw new ArgumentNullException(nameof(value));
        advanceToValueState(name);
        doWriteExtDouble(value, style ?? NumberStyles.Simple);
        setNextState();
    }

    public void WriteExtString(TName name, DsonExtString value, StringStyle style = StringStyle.Auto) {
        if (value == null) throw new ArgumentNullException(nameof(value));
        advanceToValueState(name);
        doWriteExtString(value, style);
        setNextState();
    }

    public void WriteRef(TName name, ObjectRef objectRef) {
        advanceToValueState(name);
        doWriteRef(objectRef);
        setNextState();
    }

    public void WriteTimestamp(TName name, OffsetTimestamp timestamp) {
        advanceToValueState(name);
        doWriteTimestamp(timestamp);
        setNextState();
    }

    protected abstract void doWriteInt32(int value, WireType wireType, INumberStyle style);

    protected abstract void doWriteInt64(long value, WireType wireType, INumberStyle style);

    protected abstract void doWriteFloat(float value, INumberStyle style);

    protected abstract void doWriteDouble(double value, INumberStyle style);

    protected abstract void doWriteBool(bool value);

    protected abstract void doWriteString(String value, StringStyle style);

    protected abstract void doWriteNull();

    protected abstract void doWriteBinary(DsonBinary binary);

    protected abstract void doWriteBinary(int type, DsonChunk chunk);

    protected abstract void doWriteExtInt32(DsonExtInt32 extInt32, WireType wireType, INumberStyle style);

    protected abstract void doWriteExtInt64(DsonExtInt64 extInt64, WireType wireType, INumberStyle style);

    protected abstract void doWriteExtDouble(DsonExtDouble extDouble, INumberStyle style);

    protected abstract void doWriteExtString(DsonExtString extString, StringStyle style);

    protected abstract void doWriteRef(ObjectRef objectRef);

    protected abstract void doWriteTimestamp(OffsetTimestamp timestamp);

    #endregion

    #region 容器

    public void WriteStartArray(ObjectStyle style) {
        WriteStartContainer(DsonContextType.ARRAY, DsonType.ARRAY, style);
    }

    public void WriteEndArray() {
        writeEndContainer(DsonContextType.ARRAY, DsonWriterState.VALUE);
    }

    public void WriteStartObject(ObjectStyle style) {
        WriteStartContainer(DsonContextType.OBJECT, DsonType.OBJECT, style);
    }

    public void WriteEndObject() {
        writeEndContainer(DsonContextType.OBJECT, DsonWriterState.NAME);
    }

    public void WriteStartHeader(ObjectStyle style) {
        // object下默认是name状态
        Context context = this._context;
        if (context.contextType == DsonContextType.OBJECT && context.state == DsonWriterState.NAME) {
            context.setState(DsonWriterState.VALUE);
        }
        WriteStartContainer(DsonContextType.HEADER, DsonType.HEADER, style);
    }

    public void WriteEndHeader() {
        writeEndContainer(DsonContextType.HEADER, DsonWriterState.NAME);
    }

    private void WriteStartContainer(DsonContextType contextType, DsonType dsonType, ObjectStyle style) {
        if (RecursionDepth >= Settings.recursionLimit) {
            throw DsonIOException.recursionLimitExceeded();
        }
        Context context = this._context;
        autoStartTopLevel(context);
        ensureValueState(context);
        doWriteStartContainer(contextType, dsonType, style);
        setNextState(); // 设置新上下文状态
    }

    private void writeEndContainer(DsonContextType contextType, DsonWriterState expectedState) {
        Context context = this._context;
        checkEndContext(context, contextType, expectedState);
        doWriteEndContainer();
        setNextState(); // parent前进一个状态
    }

    protected void autoStartTopLevel(Context context) {
        if (context.contextType == DsonContextType.TOP_LEVEL
            && context.state == DsonWriterState.INITIAL) {
            context.setState(DsonWriterState.VALUE);
        }
    }

    protected void checkEndContext(Context context, DsonContextType contextType, DsonWriterState state) {
        if (context.contextType != contextType) {
            throw DsonIOException.contextError(contextType, context.contextType);
        }
        if (context.state != state) {
            throw invalidState(DsonInternals.NewList(state), context.state);
        }
    }

    /** 写入类型信息，创建新上下文，压入上下文 */
    protected abstract void doWriteStartContainer(DsonContextType contextType, DsonType dsonType, ObjectStyle style);

    /** 弹出上下文 */
    protected abstract void doWriteEndContainer();

    #endregion

    #region 特殊

    public virtual void WriteSimpleHeader(string clsName) {
        if (clsName == null) throw new ArgumentNullException(nameof(clsName));
        IDsonWriter<string> textWrite = (IDsonWriter<string>)this;
        textWrite.WriteStartHeader();
        textWrite.WriteString(DsonHeaderFields.NAMES_CLASS_NAME, clsName, StringStyle.AUTO_QUOTE);
        textWrite.WriteEndHeader();
    }

    public void WriteMessage(TName name, int binaryType, IMessage message) {
        Dsons.CheckSubType(binaryType);
        advanceToValueState(name);
        doWriteMessage(binaryType, message);
        setNextState();
    }

    public void WriteValueBytes(TName name, DsonType type, byte[] data) {
        DsonReaderUtils.checkWriteValueAsBytes(type);
        advanceToValueState(name);
        doWriteValueBytes(type, data);
        setNextState();
    }

    public object? Attach(object userData) {
        return _context.Attach(userData);
    }

    public object? Attachment() {
        return _context.userData;
    }

    protected abstract void doWriteMessage(int binaryType, IMessage message);

    protected abstract void doWriteValueBytes(DsonType type, byte[] data);

    #endregion

    #region contxt

#nullable enable
    protected internal class Context
    {
#nullable disable
        protected internal Context _parent;
        protected internal DsonContextType contextType;
        protected internal DsonType dsonType; // 用于在Object/Array模式下写入内置数据结构
        protected internal DsonWriterState state = DsonWriterState.INITIAL;
        protected internal TName curName;
        protected internal object userData;
#nullable enable

        public Context() {
        }

        public Context init(Context? parent, DsonContextType contextType, DsonType dsonType) {
            this._parent = parent;
            this.contextType = contextType;
            this.dsonType = dsonType;
            return this;
        }

        public virtual void reset() {
            _parent = null;
            contextType = default;
            dsonType = DsonTypes.INVALID;
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
        public void setState(DsonWriterState state) {
            this.state = state;
        }

        /** 子类可隐藏 */
        public Context Parent => _parent;
    }

    #endregion
}