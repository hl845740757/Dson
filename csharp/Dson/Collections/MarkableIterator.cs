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

using System.Collections;

namespace Dson.Collections;

/// <summary>
/// C#和Java的迭代器差异较大
/// </summary>
/// <typeparam name="TE"></typeparam>
public class MarkableIterator<TE> : IEnumerator<TE>
{
    private readonly IEnumerator<TE> _baseIterator;
    private bool _marking;

    private readonly List<TE> _buffer = new(4);
    private int _bufferIndex;
    /** 用于避免List频繁删除队首 */
    private int _bufferOffsetIdx;

    private TE? _current;
    private TE? _markedValue;

    public MarkableIterator(IEnumerator<TE> baseIterator) {
        this._baseIterator = baseIterator ?? throw new ArgumentNullException(nameof(baseIterator));
        this._bufferIndex = -1;
        this._bufferOffsetIdx = -1;
        this._marking = false;
    }

    public bool IsMarking => _marking;

    /// <summary>
    /// 标记需要重置的位置
    /// </summary>
    /// <param name="overwrite">是否允许覆盖当前的mark</param>
    public void Mark(bool overwrite = false) {
        if (_marking && !overwrite) throw new InvalidOperationException();
        _marking = true;
        _markedValue = _current;
        // 丢弃缓存的数据
        for (int i = _bufferOffsetIdx + 1; i <= _bufferIndex; i++) {
            _buffer[i] = default;
        }
    }

    /// <summary>
    /// reset只重置到mark的位置
    /// </summary>
    public void Reset() {
        if (!_marking) throw new InvalidOperationException();
        _marking = false;
        _bufferIndex = _bufferOffsetIdx;
        _current = _markedValue;
    }

    /// <summary>
    /// 测试是否有下一个元素
    /// </summary>
    /// <returns></returns>
    public bool HasNext() {
        // 记录
        var prev = _current;
        var marking = _marking;
        if (!marking) {
            _marking = true;
        }
        bool hasNext = MoveNext();
        if (hasNext) {
            _bufferIndex--;
        }
        // 还原
        _current = prev;
        _marking = marking;
        return hasNext;
    }

    /// <summary>
    /// 获取下一个元素
    /// </summary>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
    public TE Next() {
        if (MoveNext()) {
            return _current;
        }
        throw new InvalidOperationException();
    }

    public void ForEachRemaining(Action<TE> action) {
        while (MoveNext()) {
            action.Invoke(_current);
        }
    }

    object IEnumerator.Current => Current;

    public TE Current => _current;

    public bool MoveNext() {
        List<TE> buffer = this._buffer;
        if (_bufferIndex + 1 < buffer.Count) {
            _current = buffer[++_bufferIndex];
            if (!_marking) {
                buffer[++_bufferOffsetIdx] = default; // 使用双指针法避免频繁的拷贝
                if (_bufferOffsetIdx >= buffer.Count || _bufferOffsetIdx >= 8) {
                    buffer.RemoveRange(0, _bufferOffsetIdx);
                    _bufferOffsetIdx = -1;
                    _bufferIndex = -1;
                }
            }
            return true;
        }
        else {
            if (_baseIterator.MoveNext()) {
                _current = _baseIterator.Current;
                if (_marking) { // 所有读取的值要保存下来
                    buffer.Add(_current);
                    _bufferIndex++;
                }
                return true;
            }
            _current = default;
            return false;
        }
    }

    public void Dispose() {
        _marking = false;
        _buffer.Clear();
        _bufferIndex = -1;
        _bufferOffsetIdx = -1;
    }
}