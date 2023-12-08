﻿/*
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

using System.Diagnostics;
using Dson.IO;
using Dson.Types;
using Google.Protobuf;

namespace Dson;

public abstract class AbstractDsonReader<TName> : IDsonReader<TName> where TName : IEquatable<TName>
{
    protected readonly DsonReaderSettings Settings;

#nullable disable
    protected internal Context _context;
    private Context _pooledContext; // 一个额外的缓存，用于写集合等减少上下文创建

    protected int _recursionDepth; // 这些值放外面，不需要上下文隔离，但需要能恢复
    protected DsonType _currentDsonType = DsonTypes.Invalid;
    protected WireType _currentWireType;
    protected int _currentWireTypeBits;
    protected internal TName _currentName;
#nullable enable

    protected AbstractDsonReader(DsonReaderSettings settings) {
        Settings = settings;
    }

    public DsonReaderSettings ReaderSettings => Settings;

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

    public virtual void Dispose() {
        _context = null!;
        _pooledContext = null;
    }

    #region state

    public DsonContextType ContextType => _context._contextType;

    public DsonType CurrentDsonType {
        get {
            if (_currentDsonType == DsonTypes.Invalid) {
                Debug.Assert(_context._contextType == DsonContextType.TopLevel);
                throw InvalidState(DsonInternals.NewList(DsonReaderState.Name, DsonReaderState.Value));
            }
            return _currentDsonType;
        }
    }

    public TName CurrentName {
        get {
            if (_context._state != DsonReaderState.Value) {
                throw InvalidState(DsonInternals.NewList(DsonReaderState.Value));
            }
            return _currentName;
        }
    }

    public bool IsAtType {
        get {
            if (_context._state == DsonReaderState.Type) {
                return true;
            }
            return _context._contextType == DsonContextType.TopLevel
                   && _context._state == DsonReaderState.Initial;
        }
    }

    public bool IsAtName => _context._state == DsonReaderState.Name;

    public bool IsAtValue => _context._state == DsonReaderState.Value;

    public TName ReadName() {
        if (_context._state != DsonReaderState.Name) {
            throw InvalidState(DsonInternals.NewList(DsonReaderState.Name));
        }
        DoReadName();
        _context.SetState(DsonReaderState.Value);
        return _currentName;
    }

    public void ReadName(TName expected) {
        // 不直接使用方法返回值比较，避免装箱
        ReadName();
        if (!expected.Equals(_currentName)) {
            throw DsonIOException.unexpectedName(expected, _currentName);
        }
    }

    public abstract DsonType ReadDsonType();

    public abstract DsonType PeekDsonType();

    protected abstract void DoReadName();

    /** 检查是否可以执行{@link #readDsonType()} */
    protected void CheckReadDsonTypeState(Context context) {
        if (context._contextType == DsonContextType.TopLevel) {
            if (context._state != DsonReaderState.Initial && context._state != DsonReaderState.Type) {
                throw InvalidState(DsonInternals.NewList(DsonReaderState.Initial, DsonReaderState.Type));
            }
        }
        else if (context._state != DsonReaderState.Type) {
            throw InvalidState(DsonInternals.NewList(DsonReaderState.Type));
        }
    }

    /** 处理读取dsonType后的状态切换 */
    protected void OnReadDsonType(Context context, DsonType dsonType) {
        if (dsonType == DsonType.EndOfObject) {
            // readEndXXX都是子上下文中执行的，因此正常情况下topLevel不会读取到 endOfObject 标记
            // 顶层读取到 END_OF_OBJECT 表示到达文件尾
            if (context._contextType == DsonContextType.TopLevel) {
                context.SetState(DsonReaderState.EndOfFile);
            }
            else {
                context.SetState(DsonReaderState.WaitEndObject);
            }
        }
        else {
            // topLevel只可是容器对象
            if (context._contextType == DsonContextType.TopLevel && !dsonType.IsContainerOrHeader()) {
                throw DsonIOException.invalidDsonType(context._contextType, dsonType);
            }
            if (context._contextType == DsonContextType.Object) {
                // 如果是header则直接进入VALUE状态 - header是匿名属性
                if (dsonType == DsonType.Header) {
                    context.SetState(DsonReaderState.Value);
                }
                else {
                    context.SetState(DsonReaderState.Name);
                }
            }
            else if (context._contextType == DsonContextType.Header) {
                context.SetState(DsonReaderState.Name);
            }
            else {
                context.SetState(DsonReaderState.Value);
            }
        }
    }

    /** 前进到读值状态 */
    protected void AdvanceToValueState(TName name, DsonType requiredType) {
        Context context = this._context;
        if (context._state != DsonReaderState.Value) {
            if (context._state == DsonReaderState.Type) {
                ReadDsonType();
            }
            if (context._state == DsonReaderState.Name) {
                ReadName(name);
            }
            if (context._state != DsonReaderState.Value) {
                throw InvalidState(DsonInternals.NewList(DsonReaderState.Value));
            }
        }
        if (requiredType != DsonTypes.Invalid && _currentDsonType != requiredType) {
            throw DsonIOException.dsonTypeMismatch(requiredType, _currentDsonType);
        }
    }

    protected void EnsureValueState(Context context, DsonType requiredType) {
        if (context._state != DsonReaderState.Value) {
            throw InvalidState(DsonInternals.NewList(DsonReaderState.Value));
        }
        if (_currentDsonType != requiredType) {
            throw DsonIOException.dsonTypeMismatch(requiredType, _currentDsonType);
        }
    }

    protected void SetNextState() {
        _context.SetState(DsonReaderState.Type);
    }

    protected DsonIOException InvalidState(List<DsonReaderState> expected) {
        return DsonIOException.invalidState(_context._contextType, expected, _context._state);
    }

    #endregion

    #region 简单值

    public int ReadInt32(TName name) {
        AdvanceToValueState(name, DsonType.Int32);
        int value = DoReadInt32();
        SetNextState();
        return value;
    }

    public long ReadInt64(TName name) {
        AdvanceToValueState(name, DsonType.Int64);
        long value = DoReadInt64();
        SetNextState();
        return value;
    }

    public float ReadFloat(TName name) {
        AdvanceToValueState(name, DsonType.Float);
        float value = DoReadFloat();
        SetNextState();
        return value;
    }

    public double ReadDouble(TName name) {
        AdvanceToValueState(name, DsonType.Double);
        double value = DoReadDouble();
        SetNextState();
        return value;
    }

    public bool ReadBoolean(TName name) {
        AdvanceToValueState(name, DsonType.Boolean);
        bool value = DoReadBool();
        SetNextState();
        return value;
    }

    public string ReadString(TName name) {
        AdvanceToValueState(name, DsonType.String);
        String value = DoReadString();
        SetNextState();
        return value;
    }

    public void ReadNull(TName name) {
        AdvanceToValueState(name, DsonType.Null);
        DoReadNull();
        SetNextState();
    }

    public DsonBinary ReadBinary(TName name) {
        AdvanceToValueState(name, DsonType.Binary);
        DsonBinary value = DoReadBinary();
        SetNextState();
        return value;
    }

    public DsonExtInt32 ReadExtInt32(TName name) {
        AdvanceToValueState(name, DsonType.ExtInt32);
        DsonExtInt32 value = DoReadExtInt32();
        SetNextState();
        return value;
    }

    public DsonExtInt64 ReadExtInt64(TName name) {
        AdvanceToValueState(name, DsonType.ExtInt64);
        DsonExtInt64 value = DoReadExtInt64();
        SetNextState();
        return value;
    }

    public DsonExtDouble ReadExtDouble(TName name) {
        AdvanceToValueState(name, DsonType.ExtDouble);
        DsonExtDouble value = DoReadExtDouble();
        SetNextState();
        return value;
    }

    public DsonExtString ReadExtString(TName name) {
        AdvanceToValueState(name, DsonType.ExtString);
        DsonExtString value = DoReadExtString();
        SetNextState();
        return value;
    }

    public ObjectRef ReadRef(TName name) {
        AdvanceToValueState(name, DsonType.Reference);
        ObjectRef value = DoReadRef();
        SetNextState();
        return value;
    }

    public OffsetTimestamp ReadTimestamp(TName name) {
        AdvanceToValueState(name, DsonType.Timestamp);
        OffsetTimestamp value = DoReadTimestamp();
        SetNextState();
        return value;
    }

    protected abstract int DoReadInt32();

    protected abstract long DoReadInt64();

    protected abstract float DoReadFloat();

    protected abstract double DoReadDouble();

    protected abstract bool DoReadBool();

    protected abstract string DoReadString();

    protected abstract void DoReadNull();

    protected abstract DsonBinary DoReadBinary();

    protected abstract DsonExtInt32 DoReadExtInt32();

    protected abstract DsonExtInt64 DoReadExtInt64();

    protected abstract DsonExtDouble DoReadExtDouble();

    protected abstract DsonExtString DoReadExtString();

    protected abstract ObjectRef DoReadRef();

    protected abstract OffsetTimestamp DoReadTimestamp();

    #endregion

    #region 容器

    public void ReadStartArray() {
        ReadStartContainer(DsonContextType.Array, DsonType.Array);
    }

    public void ReadEndArray() {
        ReadEndContainer(DsonContextType.Array);
    }

    public void ReadStartObject() {
        ReadStartContainer(DsonContextType.Object, DsonType.Object);
    }

    public void ReadEndObject() {
        ReadEndContainer(DsonContextType.Object);
    }

    public void ReadStartHeader() {
        ReadStartContainer(DsonContextType.Header, DsonType.Header);
    }

    public void ReadEndHeader() {
        ReadEndContainer(DsonContextType.Header);
    }

    public void BackToWaitStart() {
        Context context = this._context;
        if (context._contextType == DsonContextType.TopLevel) {
            throw DsonIOException.contextErrorTopLevel();
        }
        if (context._state != DsonReaderState.Type) {
            throw InvalidState(DsonInternals.NewList(DsonReaderState.Type));
        }
        context.SetState(DsonReaderState.WaitStartObject);
    }

    private void ReadStartContainer(DsonContextType contextType, DsonType dsonType) {
        Context context = this._context;
        if (context._state == DsonReaderState.WaitStartObject) {
            SetNextState();
            return;
        }
        if (_recursionDepth >= Settings.RecursionLimit) {
            throw DsonIOException.recursionLimitExceeded();
        }
        AutoStartTopLevel(context);
        EnsureValueState(context, dsonType);
        DoReadStartContainer(contextType, dsonType);
        SetNextState(); // 设置新上下文状态
    }

    private void ReadEndContainer(DsonContextType contextType) {
        Context context = this._context;
        CheckEndContext(context, contextType);
        DoReadEndContainer();
        SetNextState(); // parent前进一个状态
    }

    private void AutoStartTopLevel(Context context) {
        if (context._contextType == DsonContextType.TopLevel
            && (context._state == DsonReaderState.Initial || context._state == DsonReaderState.Type)) {
            ReadDsonType();
        }
    }

    private void CheckEndContext(Context context, DsonContextType contextType) {
        if (context._contextType != contextType) {
            throw DsonIOException.contextError(contextType, context._contextType);
        }
        if (context._state != DsonReaderState.WaitEndObject) {
            throw InvalidState(DsonInternals.NewList(DsonReaderState.WaitEndObject));
        }
    }

    /** 限用于读取容器后恢复上下文 */
    protected void RecoverDsonType(Context context) {
        this._currentDsonType = context._dsonType;
        this._currentWireType = WireType.VarInt;
        this._currentWireTypeBits = 0;
        this._currentName = context._name;
    }

    /** 创建新的context，保存信息，压入上下文 */
    protected abstract void DoReadStartContainer(DsonContextType contextType, DsonType dsonType);

    /** 恢复到旧的上下文，恢复{@link #currentDsonType}，弹出上下文 */
    protected abstract void DoReadEndContainer();

    #endregion

    #region 特殊

    public void SkipName() {
        Context context = GetContext();
        if (context._state == DsonReaderState.Value) {
            return;
        }
        if (context._state != DsonReaderState.Name) {
            throw InvalidState(DsonInternals.NewList(DsonReaderState.Value, DsonReaderState.Name));
        }
        DoSkipName();
        _currentName = default;
        context.SetState(DsonReaderState.Value);
    }

    public void SkipValue() {
        if (_context._state != DsonReaderState.Value) {
            throw InvalidState(DsonInternals.NewList(DsonReaderState.Value));
        }
        DoSkipValue();
        SetNextState();
    }

    public void SkipToEndOfObject() {
        Context context = GetContext();
        if (context._contextType == DsonContextType.TopLevel) {
            throw DsonIOException.contextErrorTopLevel();
        }
        if (context._state == DsonReaderState.WaitStartObject) {
            throw InvalidState(DsonInternals.NewList(DsonReaderState.Type, DsonReaderState.Name, DsonReaderState.Value));
        }
        if (_currentDsonType == DsonType.EndOfObject) {
            Debug.Assert(context._state == DsonReaderState.WaitEndObject);
            return;
        }
        DoSkipToEndOfObject();
        SetNextState();
        ReadDsonType(); // end of object
        Debug.Assert(_currentDsonType == DsonType.EndOfObject);
    }

    public T ReadMessage<T>(TName name, int binaryType, MessageParser<T> parser) where T : IMessage<T> {
        if (parser == null) throw new ArgumentNullException(nameof(parser));
        AdvanceToValueState(name, DsonType.Binary);
        T value = DoReadMessage(binaryType, parser);
        SetNextState();
        return value;
    }

    public byte[] ReadValueAsBytes(TName name) {
        AdvanceToValueState(name, DsonTypes.Invalid);
        DsonReaderUtils.CheckReadValueAsBytes(_currentDsonType);
        byte[] data = DoReadValueAsBytes();
        SetNextState();
        return data;
    }

    public object? Attach(object? userData) {
        return _context.Attach(userData);
    }

    public object Attachment() {
        return _context._userData;
    }

    protected abstract void DoSkipName();

    protected abstract void DoSkipValue();

    protected abstract void DoSkipToEndOfObject();

    protected abstract T DoReadMessage<T>(int binaryType, MessageParser<T> parser) where T : IMessage<T>;

    protected abstract byte[] DoReadValueAsBytes();

    #endregion

    #region context

    protected internal class Context
    {
#nullable disable
        protected internal Context _parent;
        protected internal DsonContextType _contextType;
        protected internal DsonType _dsonType = DsonTypes.Invalid;
        protected internal DsonReaderState _state = DsonReaderState.Initial;
        protected internal TName _name;
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
            _name = default;
            _userData = null;
        }

        public object? Attach(object? userData) {
            var r = this._userData;
            this._userData = userData;
            return r;
        }

        /** 方便查看赋值的调用 */
        public void SetState(DsonReaderState state) {
            this._state = state;
        }

        public Context Parent => _parent;
    }

    #endregion
}