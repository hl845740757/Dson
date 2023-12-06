using System.Diagnostics;
using Dson.IO;
using Dson.Types;
using Google.Protobuf;

namespace Dson;

public abstract class AbstractDsonReader<TName> : IDsonReader<TName> where TName : IEquatable<TName>
{
    protected readonly DsonReaderSettings Settings;
    protected readonly AbstractDsonReader<string>? textReader;
    protected readonly AbstractDsonReader<int>? binReader;

    internal Context _context;
    private Context? _pooledContext; // 一个额外的缓存，用于写集合等减少上下文创建

    // 这些值放外面，不需要上下文隔离，但需要能恢复
    protected internal int recursionDepth;
    protected internal DsonType currentDsonType = DsonTypeExt.INVALID;
    protected internal WireType currentWireType;
    protected internal int currentWireTypeBits;
    protected internal TName currentName;

    protected AbstractDsonReader(DsonReaderSettings settings) {
        Settings = settings;
        if (DsonInternals.IsStringKey<TName>()) {
            this.textReader = this as AbstractDsonReader<string>;
            this.binReader = null;
        }
        else {
            this.textReader = null;
            this.binReader = this as AbstractDsonReader<int>;
        }
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

    public DsonContextType ContextType => _context.contextType;

    public DsonType CurrentDsonType {
        get {
            if (currentDsonType == DsonTypeExt.INVALID) {
                Debug.Assert(_context.contextType == DsonContextType.TOP_LEVEL);
                throw invalidState(DsonInternals.NewList(DsonReaderState.NAME, DsonReaderState.VALUE));
            }
            return currentDsonType;
        }
    }

    public TName CurrentName {
        get {
            if (_context.state != DsonReaderState.VALUE) {
                throw invalidState(DsonInternals.NewList(DsonReaderState.VALUE));
            }
            return currentName;
        }
    }

    public bool IsAtType {
        get {
            if (_context.state == DsonReaderState.TYPE) {
                return true;
            }
            return _context.contextType == DsonContextType.TOP_LEVEL
                   && _context.state == DsonReaderState.INITIAL;
        }
    }

    public bool IsAtName => _context.state == DsonReaderState.NAME;

    public bool IsAtValue => _context.state == DsonReaderState.VALUE;

    public TName ReadName() {
        if (_context.state != DsonReaderState.NAME) {
            throw invalidState(DsonInternals.NewList(DsonReaderState.NAME));
        }
        doReadName();
        _context.setState(DsonReaderState.VALUE);
        return currentName;
    }

    public void ReadName(TName expected) {
        // 不直接使用方法返回值比较，避免装箱
        ReadName();
        if (!expected.Equals(currentName)) {
            throw DsonIOException.unexpectedName(expected, currentName);
        }
    }

    public abstract DsonType ReadDsonType();

    public abstract DsonType PeekDsonType();

    protected abstract void doReadName();

    /** 检查是否可以执行{@link #readDsonType()} */
    protected void checkReadDsonTypeState(Context context) {
        if (context.contextType == DsonContextType.TOP_LEVEL) {
            if (context.state != DsonReaderState.INITIAL && context.state != DsonReaderState.TYPE) {
                throw invalidState(DsonInternals.NewList(DsonReaderState.INITIAL, DsonReaderState.TYPE));
            }
        }
        else if (context.state != DsonReaderState.TYPE) {
            throw invalidState(DsonInternals.NewList(DsonReaderState.TYPE));
        }
    }

    /** 处理读取dsonType后的状态切换 */
    protected void onReadDsonType(Context context, DsonType dsonType) {
        if (dsonType == DsonType.END_OF_OBJECT) {
            // readEndXXX都是子上下文中执行的，因此正常情况下topLevel不会读取到 endOfObject 标记
            // 顶层读取到 END_OF_OBJECT 表示到达文件尾
            if (context.contextType == DsonContextType.TOP_LEVEL) {
                context.setState(DsonReaderState.END_OF_FILE);
            }
            else {
                context.setState(DsonReaderState.WAIT_END_OBJECT);
            }
        }
        else {
            // topLevel只可是容器对象
            if (context.contextType == DsonContextType.TOP_LEVEL && !dsonType.IsContainerOrHeader()) {
                throw DsonIOException.invalidDsonType(context.contextType, dsonType);
            }
            if (context.contextType == DsonContextType.OBJECT) {
                // 如果是header则直接进入VALUE状态 - header是匿名属性
                if (dsonType == DsonType.HEADER) {
                    context.setState(DsonReaderState.VALUE);
                }
                else {
                    context.setState(DsonReaderState.NAME);
                }
            }
            else if (context.contextType == DsonContextType.HEADER) {
                context.setState(DsonReaderState.NAME);
            }
            else {
                context.setState(DsonReaderState.VALUE);
            }
        }
    }

    /** 前进到读值状态 */
    protected void advanceToValueState(TName name, DsonType requiredType) {
        Context context = this._context;
        if (context.state != DsonReaderState.VALUE) {
            if (context.state == DsonReaderState.TYPE) {
                ReadDsonType();
            }
            if (context.state == DsonReaderState.NAME) {
                ReadName(name);
            }
            if (context.state != DsonReaderState.VALUE) {
                throw invalidState(DsonInternals.NewList(DsonReaderState.VALUE));
            }
        }
        if (requiredType != DsonTypeExt.INVALID && currentDsonType != requiredType) {
            throw DsonIOException.dsonTypeMismatch(requiredType, currentDsonType);
        }
    }

    protected void ensureValueState(Context context, DsonType requiredType) {
        if (context.state != DsonReaderState.VALUE) {
            throw invalidState(DsonInternals.NewList(DsonReaderState.VALUE));
        }
        if (currentDsonType != requiredType) {
            throw DsonIOException.dsonTypeMismatch(requiredType, currentDsonType);
        }
    }

    protected void setNextState() {
        _context.setState(DsonReaderState.TYPE);
    }

    protected DsonIOException invalidState(List<DsonReaderState> expected) {
        return DsonIOException.invalidState(_context.contextType, expected, _context.state);
    }

    #endregion

    #region 简单值

    public int ReadInt32(TName name) {
        advanceToValueState(name, DsonType.INT32);
        int value = doReadInt32();
        setNextState();
        return value;
    }

    public long ReadInt64(TName name) {
        advanceToValueState(name, DsonType.INT64);
        long value = doReadInt64();
        setNextState();
        return value;
    }

    public float ReadFloat(TName name) {
        advanceToValueState(name, DsonType.FLOAT);
        float value = doReadFloat();
        setNextState();
        return value;
    }

    public double ReadDouble(TName name) {
        advanceToValueState(name, DsonType.DOUBLE);
        double value = doReadDouble();
        setNextState();
        return value;
    }

    public bool ReadBoolean(TName name) {
        advanceToValueState(name, DsonType.BOOLEAN);
        bool value = doReadBool();
        setNextState();
        return value;
    }

    public string ReadString(TName name) {
        advanceToValueState(name, DsonType.STRING);
        String value = doReadString();
        setNextState();
        return value;
    }

    public void ReadNull(TName name) {
        advanceToValueState(name, DsonType.NULL);
        doReadNull();
        setNextState();
    }

    public DsonBinary ReadBinary(TName name) {
        advanceToValueState(name, DsonType.BINARY);
        DsonBinary value = doReadBinary();
        setNextState();
        return value;
    }

    public DsonExtInt32 ReadExtInt32(TName name) {
        advanceToValueState(name, DsonType.EXT_INT32);
        DsonExtInt32 value = doReadExtInt32();
        setNextState();
        return value;
    }

    public DsonExtInt64 ReadExtInt64(TName name) {
        advanceToValueState(name, DsonType.EXT_INT64);
        DsonExtInt64 value = doReadExtInt64();
        setNextState();
        return value;
    }

    public DsonExtDouble ReadExtDouble(TName name) {
        advanceToValueState(name, DsonType.EXT_DOUBLE);
        DsonExtDouble value = doReadExtDouble();
        setNextState();
        return value;
    }

    public DsonExtString ReadExtString(TName name) {
        advanceToValueState(name, DsonType.EXT_STRING);
        DsonExtString value = doReadExtString();
        setNextState();
        return value;
    }

    public ObjectRef ReadRef(TName name) {
        advanceToValueState(name, DsonType.REFERENCE);
        ObjectRef value = doReadRef();
        setNextState();
        return value;
    }

    public OffsetTimestamp ReadTimestamp(TName name) {
        advanceToValueState(name, DsonType.TIMESTAMP);
        OffsetTimestamp value = doReadTimestamp();
        setNextState();
        return value;
    }

    protected abstract int doReadInt32();

    protected abstract long doReadInt64();

    protected abstract float doReadFloat();

    protected abstract double doReadDouble();

    protected abstract bool doReadBool();

    protected abstract String doReadString();

    protected abstract void doReadNull();

    protected abstract DsonBinary doReadBinary();

    protected abstract DsonExtInt32 doReadExtInt32();

    protected abstract DsonExtInt64 doReadExtInt64();

    protected abstract DsonExtDouble doReadExtDouble();

    protected abstract DsonExtString doReadExtString();

    protected abstract ObjectRef doReadRef();

    protected abstract OffsetTimestamp doReadTimestamp();

    #endregion

    #region 容器

    public void ReadStartArray() {
        ReadStartContainer(DsonContextType.ARRAY, DsonType.ARRAY);
    }

    public void ReadEndArray() {
        ReadEndContainer(DsonContextType.ARRAY);
    }

    public void ReadStartObject() {
        ReadStartContainer(DsonContextType.OBJECT, DsonType.OBJECT);
    }

    public void ReadEndObject() {
        ReadEndContainer(DsonContextType.OBJECT);
    }

    public void ReadStartHeader() {
        ReadStartContainer(DsonContextType.HEADER, DsonType.HEADER);
    }

    public void ReadEndHeader() {
        ReadEndContainer(DsonContextType.HEADER);
    }

    public void BackToWaitStart() {
        Context context = this._context;
        if (context.contextType == DsonContextType.TOP_LEVEL) {
            throw DsonIOException.contextErrorTopLevel();
        }
        if (context.state != DsonReaderState.TYPE) {
            throw invalidState(DsonInternals.NewList(DsonReaderState.TYPE));
        }
        context.setState(DsonReaderState.WAIT_START_OBJECT);
    }

    private void ReadStartContainer(DsonContextType contextType, DsonType dsonType) {
        Context context = this._context;
        if (context.state == DsonReaderState.WAIT_START_OBJECT) {
            setNextState();
            return;
        }
        if (recursionDepth >= Settings.recursionLimit) {
            throw DsonIOException.recursionLimitExceeded();
        }
        AutoStartTopLevel(context);
        ensureValueState(context, dsonType);
        doReadStartContainer(contextType, dsonType);
        setNextState(); // 设置新上下文状态
    }

    private void ReadEndContainer(DsonContextType contextType) {
        Context context = this._context;
        CheckEndContext(context, contextType);
        doReadEndContainer();
        setNextState(); // parent前进一个状态
    }

    private void AutoStartTopLevel(Context context) {
        if (context.contextType == DsonContextType.TOP_LEVEL
            && (context.state == DsonReaderState.INITIAL || context.state == DsonReaderState.TYPE)) {
            ReadDsonType();
        }
    }

    private void CheckEndContext(Context context, DsonContextType contextType) {
        if (context.contextType != contextType) {
            throw DsonIOException.contextError(contextType, context.contextType);
        }
        if (context.state != DsonReaderState.WAIT_END_OBJECT) {
            throw invalidState(DsonInternals.NewList(DsonReaderState.WAIT_END_OBJECT));
        }
    }

    /** 限用于读取容器后恢复上下文 */
    protected void recoverDsonType(Context context) {
        this.currentDsonType = context.dsonType;
        this.currentWireType = WireType.VarInt;
        this.currentWireTypeBits = 0;
        this.currentName = context.name;
    }

    /** 创建新的context，保存信息，压入上下文 */
    protected abstract void doReadStartContainer(DsonContextType contextType, DsonType dsonType);

    /** 恢复到旧的上下文，恢复{@link #currentDsonType}，弹出上下文 */
    protected abstract void doReadEndContainer();

    #endregion

    #region 特殊

    public void SkipName() {
        Context context = GetContext();
        if (context.state == DsonReaderState.VALUE) {
            return;
        }
        if (context.state != DsonReaderState.NAME) {
            throw invalidState(DsonInternals.NewList(DsonReaderState.VALUE, DsonReaderState.NAME));
        }
        doSkipName();
        currentName = default;
        context.setState(DsonReaderState.VALUE);
    }

    public void SkipValue() {
        if (_context.state != DsonReaderState.VALUE) {
            throw invalidState(DsonInternals.NewList(DsonReaderState.VALUE));
        }
        doSkipValue();
        setNextState();
    }

    public void SkipToEndOfObject() {
        Context context = GetContext();
        if (context.contextType == DsonContextType.TOP_LEVEL) {
            throw DsonIOException.contextErrorTopLevel();
        }
        if (context.state == DsonReaderState.WAIT_START_OBJECT) {
            throw invalidState(DsonInternals.NewList(DsonReaderState.TYPE, DsonReaderState.NAME, DsonReaderState.VALUE));
        }
        if (currentDsonType == DsonType.END_OF_OBJECT) {
            Debug.Assert(context.state == DsonReaderState.WAIT_END_OBJECT);
            return;
        }
        doSkipToEndOfObject();
        setNextState();
        ReadDsonType(); // end of object
        Debug.Assert(currentDsonType == DsonType.END_OF_OBJECT);
    }

    public T ReadMessage<T>(TName name, int binaryType, MessageParser<T> parser) where T : IMessage<T> {
        if (parser == null) throw new ArgumentNullException(nameof(parser));
        advanceToValueState(name, DsonType.BINARY);
        T value = doReadMessage(binaryType, parser);
        setNextState();
        return value;
    }

    public byte[] ReadValueAsBytes(TName name) {
        advanceToValueState(name, DsonTypeExt.INVALID);
        DsonReaderUtils.checkReadValueAsBytes(currentDsonType);
        byte[] data = doReadValueAsBytes();
        setNextState();
        return data;
    }

    public object? Attach(object? userData) {
        return _context.attach(userData);
    }

    public object? Attachment() {
        return _context.userData;
    }

    protected abstract void doSkipName();

    protected abstract void doSkipValue();

    protected abstract void doSkipToEndOfObject();

    protected abstract T doReadMessage<T>(int binaryType, MessageParser<T> parser) where T : IMessage<T>;

    protected abstract byte[] doReadValueAsBytes();

    #endregion

    #region context

    protected internal class Context
    {
        protected internal Context? parent;
        protected internal DsonContextType contextType;
        protected internal DsonType dsonType = DsonTypeExt.INVALID;
        protected internal DsonReaderState state = DsonReaderState.INITIAL;
        protected internal TName name = default;
        protected internal object? userData;

        public Context() {
        }

        public Context init(Context? parent, DsonContextType contextType, DsonType dsonType) {
            this.parent = parent;
            this.contextType = contextType;
            this.dsonType = dsonType;
            return this;
        }

        public virtual void reset() {
            parent = null;
            contextType = default;
            dsonType = DsonTypeExt.INVALID;
            state = default;
            name = default;
            userData = null;
        }

        public object? attach(object? userData) {
            var r = this.userData;
            this.userData = userData;
            return r;
        }

        /** 方便查看赋值的调用 */
        public void setState(DsonReaderState state) {
            this.state = state;
        }

        public Context Parent => parent;
    }

    #endregion
}