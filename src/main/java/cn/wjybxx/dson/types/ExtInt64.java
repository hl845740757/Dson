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

package cn.wjybxx.dson.types;

import cn.wjybxx.dson.Dsons;

/**
 * long值的简单扩展
 *
 * @author wjybxx
 * date - 2023/4/19
 */
public class ExtInt64 implements Comparable<ExtInt64> {

    private final int type;
    private final long value;
    private final boolean hasValue;

    public ExtInt64(int type, long value) {
        this(type, value, true);
    }

    public ExtInt64(int type, Long value) {
        this(type, value == null ? 0 : value, value != null);
    }

    public ExtInt64(int type, long value, boolean hasValue) {
        Dsons.checkSubType(type);
        Dsons.checkHasValue(value, hasValue);
        this.type = type;
        this.value = value;
        this.hasValue = hasValue;
    }

    public static ExtInt64 emptyOf(int type) {
        return new ExtInt64(type, 0, false);
    }

    public int getType() {
        return type;
    }

    public long getValue() {
        return value;
    }

    public boolean hasValue() {
        return hasValue;
    }

    //
    @Override
    public int compareTo(ExtInt64 that) {
        int r = Integer.compare(type, that.type);
        if (r != 0) {
            return r;
        }
        return Long.compare(value, that.value);
    }

    @Override
    public boolean equals(Object o) {
        if (this == o) return true;
        if (o == null || getClass() != o.getClass()) return false;

        ExtInt64 that = (ExtInt64) o;

        if (type != that.type) return false;
        return value == that.value;
    }

    @Override
    public int hashCode() {
        int result = type;
        result = 31 * result + (int) (value ^ (value >>> 32));
        return result;
    }

    @Override
    public String toString() {
        return "ExtInt64{" +
                "type=" + type +
                ", value=" + value +
                ", hasValue=" + hasValue +
                '}';
    }
}
