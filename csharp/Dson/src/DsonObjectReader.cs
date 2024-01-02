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
using System.Collections;
using System.Collections.Generic;
using Wjybxx.Dson.Collections;
using Wjybxx.Dson.IO;
using Wjybxx.Dson.Types;

#pragma warning disable CS1591
namespace Wjybxx.Dson;

/// <summary>
/// 从<see cref="DsonArray{TK}"/>中读取输入
/// </summary>
/// <typeparam name="TName"></typeparam>
public class DsonObjectReader<TName> : AbstractDsonReader<TName> where TName : IEquatable<TName>
{
    private TName? _nextName;
    private DsonValue? _nextValue;

    public DsonObjectReader(DsonReaderSettings settings, DsonArray<TName> dsonArray)
        : base(settings) {
        Context context = new Context();
        context.Init(null, DsonContextType.TopLevel, DsonTypes.Invalid);
        context.header = dsonArray.Header.Count > 0 ? dsonArray.Header : null;
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
            throw DsonIOException.ContextError(DsonInternals.NewList(DsonContextType.Object, DsonContextType.Header), context.contextType);
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
        if (context.dsonObject == null) {
            throw DsonIOException.ContextError(DsonInternals.NewList(DsonContextType.Object, DsonContextType.Header), context.contextType);
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
        CheckReadDsonTypeState(context);

        PopNextName();
        PopNextValue();

        DsonType dsonType;
        if (context.header != null) { // 需要先读取header
            dsonType = DsonType.Header;
            PushNextValue(context.header);
            context.header = null;
        } else if (context.contextType.IsLikeArray()) {
            DsonValue nextValue = context.NextValue();
            if (nextValue == null) {
                dsonType = DsonType.EndOfObject;
            } else {
                PushNextValue(nextValue);
                dsonType = nextValue.DsonType;
            }
        } else {
            KeyValuePair<TName, DsonValue>? nextElement = context.NextElement();
            if (!nextElement.HasValue) {
                dsonType = DsonType.EndOfObject;
            } else {
                PushNextName(nextElement.Value.Key);
                PushNextValue(nextElement.Value.Value);
                dsonType = nextElement.Value.Value.DsonType;
            }
        }

        this._currentDsonType = dsonType;
        this._currentWireType = WireType.VarInt;
        this._currentName = default;

        OnReadDsonType(context, dsonType);
        return dsonType;
    }

    public override DsonType PeekDsonType() {
        Context context = this.GetContext();
        CheckReadDsonTypeState(context);

        if (context.header != null) {
            return DsonType.Header;
        }
        if (!context.HasNext()) {
            return DsonType.EndOfObject;
        }
        if (context.contextType.IsLikeArray()) {
            context.MarkItr();
            DsonValue nextValue = context.NextValue();
            context.ResetItr();
            return nextValue!.DsonType;
        } else {
            context.MarkItr();
            KeyValuePair<TName, DsonValue>? nextElement = context.NextElement();
            context.ResetItr();
            return nextElement!.Value.Value.DsonType;
        }
    }

    protected override void DoReadName() {
        _currentName = PopNextName();
    }

    #endregion

    #region 简单值

    protected override int DoReadInt32() {
        return PopNextValue().AsInt32();
    }

    protected override long DoReadInt64() {
        return PopNextValue().AsInt64();
    }

    protected override float DoReadFloat() {
        return PopNextValue().AsFloat();
    }

    protected override double DoReadDouble() {
        return PopNextValue().AsDouble();
    }

    protected override bool DoReadBool() {
        return PopNextValue().AsBool();
    }

    protected override string DoReadString() {
        return PopNextValue().AsString();
    }

    protected override void DoReadNull() {
        PopNextValue();
    }

    protected override Binary DoReadBinary() {
        return PopNextValue().AsBinary().Copy(); // 需要拷贝
    }

    protected override ExtInt32 DoReadExtInt32() {
        return PopNextValue().AsExtInt32();
    }

    protected override ExtInt64 DoReadExtInt64() {
        return PopNextValue().AsExtInt64();
    }

    protected override ExtDouble DoReadExtDouble() {
        return PopNextValue().AsExtDouble();
    }

    protected override ExtString DoReadExtString() {
        return PopNextValue().AsExtString();
    }

    protected override ObjectRef DoReadRef() {
        return PopNextValue().AsReference();
    }

    protected override OffsetTimestamp DoReadTimestamp() {
        return PopNextValue().AsTimestamp();
    }

    #endregion

    #region 容器

    protected override void DoReadStartContainer(DsonContextType contextType, DsonType dsonType) {
        Context newContext = NewContext(GetContext(), contextType, dsonType);
        DsonValue dsonValue = PopNextValue();
        if (dsonValue.DsonType == DsonType.Object) {
            DsonObject<TName> dsonObject = dsonValue.AsObject<TName>();
            newContext.header = dsonObject.Header.Count > 0 ? dsonObject.Header : null;
            newContext.dsonObject = dsonObject;
            newContext.objectIterator = new MarkableIterator<KeyValuePair<TName, DsonValue>>(dsonObject.GetEnumerator());
        } else if (dsonValue.DsonType == DsonType.Array) {
            DsonArray<TName> dsonArray = dsonValue.AsArray<TName>();
            newContext.header = dsonArray.Header.Count > 0 ? dsonArray.Header : null;
            newContext.arrayIterator = new MarkableIterator<DsonValue>(dsonArray.GetEnumerator());
        } else {
            // 其它内置结构体
            newContext.dsonObject = dsonValue.AsHeader<TName>();
            newContext.objectIterator = new MarkableIterator<KeyValuePair<TName, DsonValue>>(dsonValue.AsHeader<TName>().GetEnumerator());
        }
        newContext.name = _currentName;

        this._recursionDepth++;
        SetContext(newContext);
    }

    protected override void DoReadEndContainer() {
        Context context = GetContext();

        // 恢复上下文
        RecoverDsonType(context);
        this._recursionDepth--;
        SetContext(context.parent!);
        PoolContext(context);
    }

    #endregion

    #region 特殊

    protected override void DoSkipName() {
        PopNextName();
    }


    protected override void DoSkipValue() {
        PopNextValue();
    }

    protected override void DoSkipToEndOfObject() {
        Context context = GetContext();
        context.header = null;
        if (context.arrayIterator != null) {
            context.arrayIterator.ForEachRemaining(_ => { });
        } else {
            context.objectIterator!.ForEachRemaining(_ => { });
        }
    }

    protected override byte[] DoReadValueAsBytes() {
        throw new InvalidOperationException("Unsupported operation");
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
        /** 如果不为null，则表示需要先读取header */
        protected internal DsonHeader<TName>? header;
        protected internal AbstractDsonObject<TName>? dsonObject;
        protected internal MarkableIterator<KeyValuePair<TName, DsonValue>>? objectIterator;
        protected internal MarkableIterator<DsonValue>? arrayIterator;

        public Context() {
        }

        public override void Reset() {
            base.Reset();
            header = null;
            dsonObject = null;
            objectIterator = null;
            arrayIterator = null;
        }

        public void SetKeyItr(IEnumerator<TName> keyItr, DsonValue defValue) {
            if (dsonObject == null) throw new InvalidOperationException();
            if (objectIterator!.IsMarking) throw new InvalidOperationException("reader is in marking state");

            objectIterator = new MarkableIterator<KeyValuePair<TName, DsonValue>>(new KeyIterator(dsonObject, keyItr, defValue));
        }

        public bool HasNext() {
            if (objectIterator != null) {
                return objectIterator.HasNext();
            } else {
                return arrayIterator!.HasNext();
            }
        }

        public void MarkItr() {
            if (objectIterator != null) {
                objectIterator.Mark();
            } else {
                arrayIterator!.Mark();
            }
        }

        public void ResetItr() {
            if (objectIterator != null) {
                objectIterator.Reset();
            } else {
                arrayIterator!.Reset();
            }
        }

        public DsonValue? NextValue() {
            return arrayIterator!.HasNext() ? arrayIterator.Next() : null;
        }

        public KeyValuePair<TName, DsonValue>? NextElement() {
            return objectIterator!.HasNext() ? objectIterator.Next() : null;
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
                } else {
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