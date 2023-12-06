using System.Collections;
using Dson.Collections;
using Dson.IO;
using Dson.Types;
using Google.Protobuf;

namespace Dson;

public class DsonObjectReader<TName> : AbstractDsonReader<TName> where TName : IEquatable<TName>
{
    private TName? nextName;
    private DsonValue? nextValue;

    public DsonObjectReader(DsonReaderSettings settings, DsonArray<TName> dsonArray)
        : base(settings) {
        Context context = new Context();
        context.init(null, DsonContextType.TOP_LEVEL, DsonTypeExt.INVALID);
        context.arrayIterator = new MarkableIterator<DsonValue>(dsonArray.GetEnumerator());
        SetContext(context);
    }

    /// <summary>
    /// 设置key的迭代顺序
    /// </summary>
    /// <param name="keyItr">Key的迭代器</param>
    /// <param name="defValue">key不存在时的返回值</param>
    public void SetKeyItr(IEnumerator<TName> keyItr, DsonValue defValue) {
        if (keyItr == null) throw new ArgumentNullException(nameof(keyItr));
        if (defValue == null) throw new ArgumentNullException(nameof(defValue));
        Context context = GetContext();
        if (context.dsonObject == null) {
            throw DsonIOException.contextError(DsonInternals.NewList(DsonContextType.OBJECT, DsonContextType.HEADER), context.contextType);
        }
        context.setKeyItr(keyItr, defValue);
    }

    /// <summary>
    /// 获取当前对象的所有key
    /// </summary>
    /// <returns></returns>
    /// <exception cref="DsonIOException"></exception>
    public ICollection<TName> Keys() {
        Context context = GetContext();
        if (context.dsonObject == null) {
            throw DsonIOException.contextError(DsonInternals.NewList(DsonContextType.OBJECT, DsonContextType.HEADER), context.contextType);
        }
        return context.dsonObject.Keys;
    }

    protected override Context GetContext() {
        return (Context)_context;
    }

    protected override Context? GetPooledContext() {
        return (Context)base.GetPooledContext();
    }

    #region state

    private void pushNextValue(DsonValue nextValue) {
        if (nextValue == null) throw new ArgumentNullException(nameof(nextValue));
        this.nextValue = nextValue;
    }

    private DsonValue popNextValue() {
        DsonValue r = this.nextValue;
        this.nextValue = null;
        return r;
    }

    private void pushNextName(TName nextName) {
        this.nextName = nextName;
    }

    private TName popNextName() {
        TName r = this.nextName;
        this.nextName = default;
        return r;
    }

    public override DsonType ReadDsonType() {
        Context context = GetContext();
        checkReadDsonTypeState(context);

        popNextName();
        popNextValue();

        DsonType dsonType;
        if (context.header != null) { // 需要先读取header
            dsonType = DsonType.HEADER;
            pushNextValue(context.header);
            context.header = null;
        }
        else if (context.contextType.isLikeArray()) {
            DsonValue nextValue = context.nextValue();
            if (nextValue == null) {
                dsonType = DsonType.END_OF_OBJECT;
            }
            else {
                pushNextValue(nextValue);
                dsonType = nextValue.DsonType;
            }
        }
        else {
            KeyValuePair<TName, DsonValue>? nextElement = context.nextElement();
            if (!nextElement.HasValue) {
                dsonType = DsonType.END_OF_OBJECT;
            }
            else {
                pushNextName(nextElement.Value.Key);
                pushNextValue(nextElement.Value.Value);
                dsonType = nextElement.Value.Value.DsonType;
            }
        }

        this.currentDsonType = dsonType;
        this.currentWireType = WireType.VarInt;
        this.currentName = default;

        onReadDsonType(context, dsonType);
        return dsonType;
    }

    public override DsonType PeekDsonType() {
        Context context = this.GetContext();
        checkReadDsonTypeState(context);

        if (context.header != null) {
            return DsonType.HEADER;
        }
        if (!context.hasNext()) {
            return DsonType.END_OF_OBJECT;
        }
        if (context.contextType.isLikeArray()) {
            context.markItr();
            DsonValue nextValue = context.nextValue();
            context.resetItr();
            return nextValue!.DsonType;
        }
        else {
            context.markItr();
            KeyValuePair<TName, DsonValue>? nextElement = context.nextElement();
            context.resetItr();
            return nextElement!.Value.Value.DsonType;
        }
    }

    protected override void doReadName() {
        currentName = popNextName();
    }

    #endregion

    #region 简单值

    protected override int doReadInt32() {
        return popNextValue().AsInt32();
    }

    protected override long doReadInt64() {
        return popNextValue().AsInt64();
    }

    protected override float doReadFloat() {
        return popNextValue().AsFloat();
    }

    protected override double doReadDouble() {
        return popNextValue().AsDouble();
    }

    protected override bool doReadBool() {
        return popNextValue().AsBool();
    }

    protected override string doReadString() {
        return popNextValue().AsString();
    }

    protected override void doReadNull() {
        popNextValue();
    }

    protected override DsonBinary doReadBinary() {
        return popNextValue().AsBinary().Copy(); // 需要拷贝
    }

    protected override DsonExtInt32 doReadExtInt32() {
        return popNextValue().AsExtInt32();
    }

    protected override DsonExtInt64 doReadExtInt64() {
        return popNextValue().AsExtInt64();
    }

    protected override DsonExtDouble doReadExtDouble() {
        return popNextValue().AsExtDouble();
    }

    protected override DsonExtString doReadExtString() {
        return popNextValue().AsExtString();
    }

    protected override ObjectRef doReadRef() {
        return popNextValue().AsReference();
    }

    protected override OffsetTimestamp doReadTimestamp() {
        return popNextValue().AsTimestamp();
    }

    #endregion

    #region 容器

    protected override void doReadStartContainer(DsonContextType contextType, DsonType dsonType) {
        Context newContext = NewContext(GetContext(), contextType, dsonType);
        DsonValue dsonValue = popNextValue();
        if (dsonValue.DsonType == DsonType.OBJECT) {
            DsonObject<TName> dsonObject = dsonValue.AsObject<TName>();
            newContext.header = dsonObject.Header.Count > 0 ? dsonObject.Header : null;
            newContext.dsonObject = dsonObject;
            newContext.objectIterator = new MarkableIterator<KeyValuePair<TName, DsonValue>>(dsonObject.GetEnumerator());
        }
        else if (dsonValue.DsonType == DsonType.ARRAY) {
            DsonArray<TName> dsonArray = dsonValue.AsArray<TName>();
            newContext.header = dsonArray.Header.Count > 0 ? dsonArray.Header : null;
            newContext.arrayIterator = new MarkableIterator<DsonValue>(dsonArray.GetEnumerator());
        }
        else {
            // 其它内置结构体
            newContext.dsonObject = dsonValue.AsHeader<TName>();
            newContext.objectIterator = new MarkableIterator<KeyValuePair<TName, DsonValue>>(dsonValue.AsHeader<TName>().GetEnumerator());
        }
        newContext.name = currentName;

        this.recursionDepth++;
        SetContext(newContext);
    }

    protected override void doReadEndContainer() {
        Context context = GetContext();

        // 恢复上下文
        recoverDsonType(context);
        this.recursionDepth--;
        SetContext(context.parent);
        PoolContext(context);
    }

    #endregion

    #region 特殊

    protected override void doSkipName() {
        popNextName();
    }


    protected override void doSkipValue() {
        popNextValue();
    }
    
    protected override void doSkipToEndOfObject() {
        Context context = GetContext();
        context.header = null;
        if (context.arrayIterator != null) {
            context.arrayIterator.ForEachRemaining(e => { });
        }
        else {
            context.objectIterator!.ForEachRemaining(e => { });
        }
    }

    protected override T doReadMessage<T>(int binaryType, MessageParser<T> parser) {
        DsonBinary dsonBinary = (DsonBinary)popNextValue();
        if (dsonBinary == null) throw new InvalidOperationException();
        return parser.ParseFrom(dsonBinary.Data);
    }


    protected override byte[] doReadValueAsBytes() {
        throw new InvalidOperationException("Unsupported operation");
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
        /** 如果不为null，则表示需要先读取header */
        protected internal DsonHeader<TName>? header;
        protected internal AbstractDsonObject<TName>? dsonObject;
        protected internal MarkableIterator<KeyValuePair<TName, DsonValue>>? objectIterator;
        protected internal MarkableIterator<DsonValue>? arrayIterator;

        public Context() {
        }

        public override void reset() {
            base.reset();
            header = null;
            dsonObject = null;
            objectIterator = null;
            arrayIterator = null;
        }

        public void setKeyItr(IEnumerator<TName> keyItr, DsonValue defValue) {
            if (dsonObject == null) throw new InvalidOperationException();
            if (objectIterator!.IsMarking) throw new InvalidOperationException("reader is in marking state");

            objectIterator = new MarkableIterator<KeyValuePair<TName, DsonValue>>(new KeyIterator(dsonObject, keyItr, defValue));
        }

        public bool hasNext() {
            if (objectIterator != null) {
                return objectIterator.HasNext();
            }
            else {
                return arrayIterator!.HasNext();
            }
        }

        public void markItr() {
            if (objectIterator != null) {
                objectIterator.Mark();
            }
            else {
                arrayIterator!.Mark();
            }
        }

        public void resetItr() {
            if (objectIterator != null) {
                objectIterator.Reset();
            }
            else {
                arrayIterator!.Reset();
            }
        }

        public DsonValue? nextValue() {
            return arrayIterator!.HasNext() ? arrayIterator.Next() : null;
        }

        public KeyValuePair<TName, DsonValue>? nextElement() {
            return objectIterator!.HasNext() ? objectIterator.Next() : null;
        }
    }

    private class KeyIterator : IEnumerator<KeyValuePair<TName, DsonValue>>
    {
        internal readonly AbstractDsonObject<TName> dsonObject;
        internal readonly IEnumerator<TName> keyItr;
        internal readonly DsonValue defValue;

        public KeyIterator(AbstractDsonObject<TName> dsonObject, IEnumerator<TName> keyItr, DsonValue defValue) {
            this.dsonObject = dsonObject;
            this.keyItr = new MarkableIterator<TName>(keyItr);
            this.defValue = defValue;
        }

        public bool MoveNext() {
            return keyItr.MoveNext();
        }

        public KeyValuePair<TName, DsonValue> Current {
            get {
                TName key = keyItr.Current;
                if (dsonObject.TryGetValue(key!, out DsonValue dsonValue)) {
                    return new KeyValuePair<TName, DsonValue>(key, dsonValue);
                }
                else {
                    return new KeyValuePair<TName, DsonValue>(key, defValue);
                }
            }
        }

        public void Reset() {
            keyItr.Reset();
        }

        object IEnumerator.Current => Current;

        public void Dispose() {
        }
    }

    #endregion
}