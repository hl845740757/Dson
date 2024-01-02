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

package cn.wjybxx.dson;

import cn.wjybxx.dson.types.ExtInt64;

import javax.annotation.Nonnull;
import java.util.Objects;

/**
 * long值的简单扩展
 *
 * @author wjybxx
 * date - 2023/4/19
 */
public final class DsonExtInt64 extends DsonValue implements Comparable<DsonExtInt64> {

    private final ExtInt64 extInt64;

    public DsonExtInt64(int type, long value) {
        this(new ExtInt64(type, value));
    }

    public DsonExtInt64(int type, Long value) {
        this(new ExtInt64(type, value));
    }

    public DsonExtInt64(int type, long value, boolean hasValue) {
        this(new ExtInt64(type, value, hasValue));
    }

    public DsonExtInt64(ExtInt64 extInt64) {
        this.extInt64 = Objects.requireNonNull(extInt64);
    }

    public ExtInt64 extInt64() {
        return extInt64;
    }

    @Nonnull
    @Override
    public DsonType getDsonType() {
        return DsonType.EXT_INT64;
    }

    public int getType() {
        return extInt64.getType();
    }

    public long getValue() {
        return extInt64.getValue();
    }

    public boolean hasValue() {
        return extInt64.hasValue();
    }

    // region equals
    @Override
    public int compareTo(DsonExtInt64 that) {
        return extInt64.compareTo(that.extInt64);
    }

    @Override
    public boolean equals(Object o) {
        if (this == o) return true;
        if (o == null || getClass() != o.getClass()) return false;

        DsonExtInt64 that = (DsonExtInt64) o;

        return extInt64.equals(that.extInt64);
    }

    @Override
    public int hashCode() {
        return extInt64.hashCode();
    }

    // endregion

    @Override
    public String toString() {
        return "DsonExtInt64{" +
                "extInt64=" + extInt64 +
                '}';
    }
}
