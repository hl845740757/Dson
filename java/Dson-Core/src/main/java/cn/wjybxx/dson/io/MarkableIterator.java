/*
 * Copyright 2023-2024 wjybxx(845740757@qq.com)
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

package cn.wjybxx.dson.io;

import java.util.ArrayList;
import java.util.Iterator;
import java.util.Objects;

/**
 * @author wjybxx
 * date - 2023/8/8
 */
public class MarkableIterator<E> implements Iterator<E> {

    private final Iterator<E> baseIterator;
    private boolean marking;

    private ArrayList<E> buffer;
    private int bufferIndex;
    /** 用于避免List频繁删除队首 */
    private int bufferOffset;

    public MarkableIterator(Iterator<E> baseIterator) {
        this.baseIterator = Objects.requireNonNull(baseIterator);
        this.bufferIndex = 0;
        this.marking = false;
    }

    public void mark() {
        if (marking) throw new IllegalStateException();
        marking = true;
        if (buffer == null) { // 延迟分配
            buffer = new ArrayList<>(4);
        }
    }

    public boolean isMarking() {
        return marking;
    }

    public void rewind() {
        bufferIndex = bufferOffset;
    }

    public void reset() {
        if (!marking) throw new IllegalStateException();
        marking = false;
        bufferIndex = bufferOffset;
    }

    @Override
    public boolean hasNext() {
        if (buffer != null && bufferIndex < buffer.size()) {
            return true;
        }
        return baseIterator.hasNext();
    }

    @Override
    public E next() {
        ArrayList<E> markIterator = this.buffer;
        if (markIterator == null) { // 延迟分配可能为null
            return baseIterator.next();
        }

        E value;
        if (bufferIndex < markIterator.size()) {
            value = markIterator.get(bufferIndex++);
            if (!marking) {
                markIterator.set(bufferOffset++, null); // 使用双指针法避免频繁的拷贝
                if (bufferOffset == markIterator.size() || bufferOffset >= 8) {
                    if (bufferOffset == markIterator.size()) {
                        markIterator.clear();
                    } else {
                        markIterator.subList(0, bufferOffset).clear();
                    }
                    bufferOffset = 0;
                    bufferIndex = 0;
                }
            }
        } else {
            value = baseIterator.next();
            if (marking) { // 所有读取的值要保存下来
                markIterator.add(value);
                bufferIndex++;
            }
        }
        return value;
    }

}