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

import cn.wjybxx.dson.types.ExtDouble;

import javax.annotation.Nonnull;
import java.util.Objects;

/**
 * double的简单扩展
 * double虽然支持NaN，但和hasValue的含义还是不一样。
 *
 * @author wjybxx
 * date - 2023/12/2
 */
public final class DsonExtDouble extends DsonValue implements Comparable<DsonExtDouble> {

    private final ExtDouble extDouble;

    public DsonExtDouble(int type, double value) {
        this(new ExtDouble(type, value));
    }

    public DsonExtDouble(int type, Double value) {
        this(new ExtDouble(type, value));
    }

    public DsonExtDouble(int type, double value, boolean hasValue) {
        this(new ExtDouble(type, value, hasValue));
    }

    public DsonExtDouble(ExtDouble extDouble) {
        this.extDouble = Objects.requireNonNull(extDouble);
    }

    public ExtDouble extDouble() {
        return extDouble;
    }

    @Nonnull
    @Override
    public DsonType getDsonType() {
        return DsonType.EXT_DOUBLE;
    }

    public int getType() {
        return extDouble.getType();
    }

    public double getValue() {
        return extDouble.getValue();
    }

    public boolean hasValue() {
        return extDouble.hasValue();
    }

    //region equals

    @Override
    public int compareTo(DsonExtDouble that) {
        return extDouble.compareTo(that.extDouble);
    }

    @Override
    public boolean equals(Object o) {
        if (this == o) return true;
        if (o == null || getClass() != o.getClass()) return false;

        DsonExtDouble that = (DsonExtDouble) o;

        return extDouble.equals(that.extDouble);
    }

    @Override
    public int hashCode() {
        return extDouble.hashCode();
    }

    // endregion

    @Override
    public String toString() {
        return "DsonExtDouble{" +
                "extDouble=" + extDouble +
                '}';
    }
}
