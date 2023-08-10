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

package cn.wjybxx.dson.internal;

import java.util.ArrayList;
import java.util.Iterator;
import java.util.Objects;

/**
 * @author wjybxx
 * date - 2023/8/8
 */
public class MarkableIterator<E> implements Iterator<E> {

    private final Iterator<E> baseIterator;
    private ArrayList<E> markIterator;

    private int curIndex;
    private int shiftIndex;
    private boolean marking;

    public MarkableIterator(Iterator<E> baseIterator) {
        this.baseIterator = Objects.requireNonNull(baseIterator);
        this.curIndex = 0;
        this.marking = false;
    }

    public void mark() {
        if (marking) throw new IllegalStateException();
        marking = true;
        if (markIterator == null) { // 延迟分配
            markIterator = new ArrayList<>(4);
        }
    }

    public void rewind() {
        curIndex = shiftIndex;
    }

    public void reset() {
        curIndex = shiftIndex;
        marking = false;
    }

    @Override
    public boolean hasNext() {
        if (markIterator != null && curIndex < markIterator.size()) {
            return true;
        }
        return baseIterator.hasNext();
    }

    @Override
    public E next() {
        ArrayList<E> markIterator = this.markIterator;
        if (markIterator == null) { // 延迟分配可能为null
            return baseIterator.next();
        }

        E value;
        if (curIndex < markIterator.size()) {
            value = markIterator.get(curIndex++);
            if (!marking) { // 使用双指针法避免频繁的拷贝
                markIterator.set(shiftIndex++, null);
                if (shiftIndex == markIterator.size() || shiftIndex >= 8) {
                    if (shiftIndex == markIterator.size()) {
                        markIterator.clear();
                    } else {
                        markIterator.subList(0, shiftIndex).clear();
                    }
                    shiftIndex = 0;
                    curIndex = 0;
                }
            }
        } else {
            value = baseIterator.next();
            if (marking) {
                markIterator.add(value);
                curIndex++;
            }
        }
        return value;
    }

}