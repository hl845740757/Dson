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

using System.Collections;

#pragma warning disable CS1591
namespace Wjybxx.Dson.Collections;

/// <summary>
/// C#和Java的迭代器差异较大；虽然写Java的时候更多，但不能说C#的迭代器有问题。
/// 对于单线程数据结构，先测试是否有数据，再Move体验更好；而对于并发数据结构，先Move再获取数据则更安全。
/// </summary>
/// <typeparam name="T">元素的类型</typeparam>
public class MarkableIterator<T> : IEnumerator<T>
{
    private readonly IEnumerator<T> _baseIterator;
    private bool _marking;

    private readonly List<T> _buffer = new(4);
    private int _bufferIndex;
    /** 用于避免List频繁删除队首 */
    private int _bufferOffsetIdx;

    private T? _current;
    private T? _markedValue;

    public MarkableIterator(IEnumerator<T> baseIterator) {
        this._baseIterator = baseIterator ?? throw new ArgumentNullException(nameof(baseIterator));
        this._bufferIndex = -1;
        this._bufferOffsetIdx = -1;
        this._marking = false;
    }

    /// <summary>
    /// 当前是否处于标记中
    /// </summary>
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
            _bufferIndex--; // 索引虽然减了，但数据保存在了Buffer中
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
    public T Next() {
        if (MoveNext()) {
            return _current;
        }
        throw new InvalidOperationException();
    }

    /// <summary>
    /// 对剩下的元素执行给定的操作
    /// </summary>
    /// <param name="action"></param>
    public void ForEachRemaining(Action<T> action) {
        while (MoveNext()) {
            action.Invoke(_current);
        }
    }

    object IEnumerator.Current => Current;

    public T Current => _current;

    /// <inheritdoc />
    public bool MoveNext() {
        List<T> buffer = this._buffer;
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

    /// <inheritdoc />
    public void Dispose() {
        _marking = false;
        _buffer.Clear();
        _bufferIndex = -1;
        _bufferOffsetIdx = -1;
    }
}