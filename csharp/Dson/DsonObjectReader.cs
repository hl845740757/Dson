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

using System.Collections;
using Dson.Collections;
using Dson.IO;
using Dson.Types;
using Google.Protobuf;

namespace Dson;

public class DsonObjectReader<TName> : AbstractDsonReader<TName> where TName : IEquatable<TName>
{
    private TName? _nextName;
    private DsonValue? _nextValue;

    public DsonObjectReader(DsonReaderSettings settings, DsonArray<TName> dsonArray)
        : base(settings) {
        Context context = new Context();
        context.init(null, DsonContextType.TOP_LEVEL, DsonTypes.INVALID);
        context.ArrayIterator = new MarkableIterator<DsonValue>(dsonArray.GetEnumerator());
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
        if (context.DsonObject == null) {
            throw DsonIOException.contextError(DsonInternals.NewList(DsonContextType.OBJECT, DsonContextType.HEADER), context.contextType);
        }
        context.SetKeyItr(keyItr, defValue);
    }

    /// <summary>
    /// 获取当前对象的所有key
    /// </summary>
    /// <returns></returns>
    /// <exception cref="DsonIOException"></exception>
    public ICollection<TName> Keys() {
        Context context = GetContext();
        if (context.DsonObject == null) {
            throw DsonIOException.contextError(DsonInternals.NewList(DsonContextType.OBJECT, DsonContextType.HEADER), context.contextType);
        }
        return context.DsonObject.Keys;
    }

    protected override Context GetContext() {
        return (Context)_context;
    }

    protected override Context? GetPooledContext() {
        return (Context)base.GetPooledContext();
    }

    #region state

    private void PushNextValue(DsonValue nextValue) {
        if (nextValue == null) throw new ArgumentNullException(nameof(nextValue));
        this._nextValue = nextValue;
    }

    private DsonValue PopNextValue() {
        DsonValue r = this._nextValue;
        this._nextValue = null;
        return r;
    }

    private void PushNextName(TName nextName) {
        this._nextName = nextName;
    }

    private TName PopNextName() {
        TName r = this._nextName;
        this._nextName = default;
        return r;
    }

    public override DsonType ReadDsonType() {
        Context context = GetContext();
        checkReadDsonTypeState(context);

        PopNextName();
        PopNextValue();

        DsonType dsonType;
        if (context.Header != null) { // 需要先读取header
            dsonType = DsonType.HEADER;
            PushNextValue(context.Header);
            context.Header = null;
        }
        else if (context.contextType.isLikeArray()) {
            DsonValue nextValue = context.NextValue();
            if (nextValue == null) {
                dsonType = DsonType.END_OF_OBJECT;
            }
            else {
                PushNextValue(nextValue);
                dsonType = nextValue.DsonType;
            }
        }
        else {
            KeyValuePair<TName, DsonValue>? nextElement = context.NextElement();
            if (!nextElement.HasValue) {
                dsonType = DsonType.END_OF_OBJECT;
            }
            else {
                PushNextName(nextElement.Value.Key);
                PushNextValue(nextElement.Value.Value);
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

        if (context.Header != null) {
            return DsonType.HEADER;
        }
        if (!context.HasNext()) {
            return DsonType.END_OF_OBJECT;
        }
        if (context.contextType.isLikeArray()) {
            context.MarkItr();
            DsonValue nextValue = context.NextValue();
            context.ResetItr();
            return nextValue!.DsonType;
        }
        else {
            context.MarkItr();
            KeyValuePair<TName, DsonValue>? nextElement = context.NextElement();
            context.ResetItr();
            return nextElement!.Value.Value.DsonType;
        }
    }

    protected override void doReadName() {
        currentName = PopNextName();
    }

    #endregion

    #region 简单值

    protected override int doReadInt32() {
        return PopNextValue().AsInt32();
    }

    protected override long doReadInt64() {
        return PopNextValue().AsInt64();
    }

    protected override float doReadFloat() {
        return PopNextValue().AsFloat();
    }

    protected override double doReadDouble() {
        return PopNextValue().AsDouble();
    }

    protected override bool doReadBool() {
        return PopNextValue().AsBool();
    }

    protected override string doReadString() {
        return PopNextValue().AsString();
    }

    protected override void doReadNull() {
        PopNextValue();
    }

    protected override DsonBinary doReadBinary() {
        return PopNextValue().AsBinary().Copy(); // 需要拷贝
    }

    protected override DsonExtInt32 doReadExtInt32() {
        return PopNextValue().AsExtInt32();
    }

    protected override DsonExtInt64 doReadExtInt64() {
        return PopNextValue().AsExtInt64();
    }

    protected override DsonExtDouble doReadExtDouble() {
        return PopNextValue().AsExtDouble();
    }

    protected override DsonExtString doReadExtString() {
        return PopNextValue().AsExtString();
    }

    protected override ObjectRef doReadRef() {
        return PopNextValue().AsReference();
    }

    protected override OffsetTimestamp doReadTimestamp() {
        return PopNextValue().AsTimestamp();
    }

    #endregion

    #region 容器

    protected override void doReadStartContainer(DsonContextType contextType, DsonType dsonType) {
        Context newContext = NewContext(GetContext(), contextType, dsonType);
        DsonValue dsonValue = PopNextValue();
        if (dsonValue.DsonType == DsonType.OBJECT) {
            DsonObject<TName> dsonObject = dsonValue.AsObject<TName>();
            newContext.Header = dsonObject.Header.Count > 0 ? dsonObject.Header : null;
            newContext.DsonObject = dsonObject;
            newContext.ObjectIterator = new MarkableIterator<KeyValuePair<TName, DsonValue>>(dsonObject.GetEnumerator());
        }
        else if (dsonValue.DsonType == DsonType.ARRAY) {
            DsonArray<TName> dsonArray = dsonValue.AsArray<TName>();
            newContext.Header = dsonArray.Header.Count > 0 ? dsonArray.Header : null;
            newContext.ArrayIterator = new MarkableIterator<DsonValue>(dsonArray.GetEnumerator());
        }
        else {
            // 其它内置结构体
            newContext.DsonObject = dsonValue.AsHeader<TName>();
            newContext.ObjectIterator = new MarkableIterator<KeyValuePair<TName, DsonValue>>(dsonValue.AsHeader<TName>().GetEnumerator());
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
        SetContext(context.parent!);
        PoolContext(context);
    }

    #endregion

    #region 特殊

    protected override void doSkipName() {
        PopNextName();
    }


    protected override void doSkipValue() {
        PopNextValue();
    }

    protected override void doSkipToEndOfObject() {
        Context context = GetContext();
        context.Header = null;
        if (context.ArrayIterator != null) {
            context.ArrayIterator.ForEachRemaining(e => { });
        }
        else {
            context.ObjectIterator!.ForEachRemaining(e => { });
        }
    }

    protected override T doReadMessage<T>(int binaryType, MessageParser<T> parser) {
        DsonBinary dsonBinary = (DsonBinary)PopNextValue();
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
        protected internal DsonHeader<TName>? Header;
        protected internal AbstractDsonObject<TName>? DsonObject;
        protected internal MarkableIterator<KeyValuePair<TName, DsonValue>>? ObjectIterator;
        protected internal MarkableIterator<DsonValue>? ArrayIterator;

        public Context() {
        }

        public override void reset() {
            base.reset();
            Header = null;
            DsonObject = null;
            ObjectIterator = null;
            ArrayIterator = null;
        }

        public void SetKeyItr(IEnumerator<TName> keyItr, DsonValue defValue) {
            if (DsonObject == null) throw new InvalidOperationException();
            if (ObjectIterator!.IsMarking) throw new InvalidOperationException("reader is in marking state");

            ObjectIterator = new MarkableIterator<KeyValuePair<TName, DsonValue>>(new KeyIterator(DsonObject, keyItr, defValue));
        }

        public bool HasNext() {
            if (ObjectIterator != null) {
                return ObjectIterator.HasNext();
            }
            else {
                return ArrayIterator!.HasNext();
            }
        }

        public void MarkItr() {
            if (ObjectIterator != null) {
                ObjectIterator.Mark();
            }
            else {
                ArrayIterator!.Mark();
            }
        }

        public void ResetItr() {
            if (ObjectIterator != null) {
                ObjectIterator.Reset();
            }
            else {
                ArrayIterator!.Reset();
            }
        }

        public DsonValue? NextValue() {
            return ArrayIterator!.HasNext() ? ArrayIterator.Next() : null;
        }

        public KeyValuePair<TName, DsonValue>? NextElement() {
            return ObjectIterator!.HasNext() ? ObjectIterator.Next() : null;
        }
    }

    private class KeyIterator : IEnumerator<KeyValuePair<TName, DsonValue>>
    {
        private readonly AbstractDsonObject<TName> _dsonObject;
        private readonly IEnumerator<TName> _keyItr;
        private readonly DsonValue _defValue;

        public KeyIterator(AbstractDsonObject<TName> dsonObject, IEnumerator<TName> keyItr, DsonValue defValue) {
            this._dsonObject = dsonObject;
            this._keyItr = new MarkableIterator<TName>(keyItr);
            this._defValue = defValue;
        }

        public bool MoveNext() {
            return _keyItr.MoveNext();
        }

        public KeyValuePair<TName, DsonValue> Current {
            get {
                TName key = _keyItr.Current;
                if (_dsonObject.TryGetValue(key!, out DsonValue dsonValue)) {
                    return new KeyValuePair<TName, DsonValue>(key, dsonValue);
                }
                else {
                    return new KeyValuePair<TName, DsonValue>(key, _defValue);
                }
            }
        }

        public void Reset() {
            _keyItr.Reset();
        }

        object IEnumerator.Current => Current;

        public void Dispose() {
        }
    }

    #endregion
}