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

package cn.wjybxx.dson;

import javax.annotation.Nonnull;

/**
 * double的简单扩展
 * double虽然支持NaN，但和hasValue的含义还是不一样。
 *
 * @author wjybxx
 * date - 2023/12/2
 */
public class DsonExtDouble extends DsonValue implements Comparable<DsonExtDouble> {

    private final int type;
    private final double value;
    private final boolean hasValue;

    public DsonExtDouble(int type, double value) {
        this(type, value, true);
    }

    public DsonExtDouble(int type, double value, boolean hasValue) {
        Dsons.checkSubType(type);
        Dsons.checkHasValue(value, hasValue);
        this.type = type;
        this.value = value;
        this.hasValue = hasValue;
    }

    public static DsonExtDouble emptyOf(int type) {
        return new DsonExtDouble(type, 0, false);
    }

    public int getType() {
        return type;
    }

    public double getValue() {
        return value;
    }

    public boolean hasValue() {
        return hasValue;
    }

    @Nonnull
    @Override
    public DsonType getDsonType() {
        return DsonType.EXT_DOUBLE;
    }

    //

    @Override
    public int compareTo(DsonExtDouble that) {
        int r = Integer.compare(type, that.type);
        if (r != 0) {
            return r;
        }
        r = Boolean.compare(hasValue, that.hasValue);
        if (r != 0) {
            return r;
        }
        return Double.compare(value, that.value);
    }

    @Override
    public boolean equals(Object o) {
        if (this == o) return true;
        if (o == null || getClass() != o.getClass()) return false;

        DsonExtDouble that = (DsonExtDouble) o;

        if (type != that.type) return false;
        if (Double.compare(value, that.value) != 0) return false;
        return hasValue == that.hasValue;
    }

    @Override
    public int hashCode() {
        int result;
        long temp;
        result = type;
        temp = Double.doubleToLongBits(value);
        result = 31 * result + (int) (temp ^ (temp >>> 32));
        result = 31 * result + (hasValue ? 1 : 0);
        return result;
    }

    @Override
    public String toString() {
        return "DsonExtDouble{" +
                "type=" + type +
                ", value=" + value +
                ", hasValue=" + hasValue +
                '}';
    }
}
