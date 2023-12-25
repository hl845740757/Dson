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
using System.Collections;
using System.Collections.Generic;
using System.Linq;

#pragma warning disable CS1591
namespace Wjybxx.Dson;

/// <summary>
/// Dson KV类结构的抽象实现
/// </summary>
/// <typeparam name="TK">String或<see cref="FieldNumber"/></typeparam>
public abstract class AbstractDsonObject<TK> : DsonValue, IDictionary<TK, DsonValue>, IEquatable<AbstractDsonObject<TK>>
{
    protected readonly IDictionary<TK, DsonValue> _valueMap;

    public AbstractDsonObject(IDictionary<TK, DsonValue> valueMap) {
        _valueMap = valueMap;
    }

    IEnumerator IEnumerable.GetEnumerator() {
        return GetEnumerator();
    }

    public IEnumerator<KeyValuePair<TK, DsonValue>> GetEnumerator() {
        return _valueMap.GetEnumerator();
    }

    #region 元素检查

    protected static void CheckElement(TK? key, DsonValue? value) {
        if (key == null) throw new ArgumentException("key cant be null");
        if (value == null) throw new ArgumentException("value cant be null");
        if (value.DsonType == DsonType.Header) throw new ArgumentException("add Header");
    }

    public DsonValue this[TK key] {
        get => _valueMap[key];
        set {
            CheckElement(key, value);
            _valueMap[key] = value;
        }
    }

    public void Add(KeyValuePair<TK, DsonValue> item) {
        CheckElement(item.Key, item.Value);
        _valueMap.Add(item);
    }

    public void Add(TK key, DsonValue value) {
        CheckElement(key, value);
        _valueMap.Add(key, value);
    }

    public virtual AbstractDsonObject<TK> Append(TK key, DsonValue value) {
        CheckElement(key, value);
        _valueMap[key!] = value;
        return this;
    }

    #endregion

    #region 简单代理

    public void Clear() {
        _valueMap.Clear();
    }

    public bool Contains(KeyValuePair<TK, DsonValue> item) {
        return _valueMap.Contains(item);
    }

    public void CopyTo(KeyValuePair<TK, DsonValue>[] array, int arrayIndex) {
        _valueMap.CopyTo(array, arrayIndex);
    }

    public bool Remove(KeyValuePair<TK, DsonValue> item) {
        return _valueMap.Remove(item);
    }

    public int Count => _valueMap.Count;
    public bool IsReadOnly => _valueMap.IsReadOnly;

    public bool ContainsKey(TK key) {
        return _valueMap.ContainsKey(key);
    }

    public bool Remove(TK key) {
        return _valueMap.Remove(key);
    }

    public bool TryGetValue(TK key, out DsonValue value) {
        return _valueMap.TryGetValue(key, out value);
    }

    public ICollection<TK> Keys => _valueMap.Keys;
    public ICollection<DsonValue> Values => _valueMap.Values;

    #endregion

    #region equals

    // 默认不比较header

    public bool Equals(AbstractDsonObject<TK>? other) {
        if (ReferenceEquals(null, other)) return false;
        if (ReferenceEquals(this, other)) return true;
        return _valueMap.SequenceEqual(other._valueMap); // C#的集合默认都是未实现Equals的
    }

    public override bool Equals(object? obj) {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != this.GetType()) return false;
        return Equals((AbstractDsonObject<TK>)obj);
    }

    public override int GetHashCode() {
        return _valueMap.GetHashCode();
    }

    public static bool operator ==(AbstractDsonObject<TK>? left, AbstractDsonObject<TK>? right) {
        return Equals(left, right);
    }

    public static bool operator !=(AbstractDsonObject<TK>? left, AbstractDsonObject<TK>? right) {
        return !Equals(left, right);
    }

    #endregion

    public override string ToString() {
        return $"{nameof(DsonType)}: {DsonType}, {nameof(_valueMap)}: {_valueMap}";
    }
}