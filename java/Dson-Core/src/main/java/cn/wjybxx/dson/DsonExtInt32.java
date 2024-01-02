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

import cn.wjybxx.dson.types.ExtInt32;

import javax.annotation.Nonnull;
import java.util.Objects;

/**
 * long值的简单扩展
 *
 * @author wjybxx
 * date - 2023/4/19
 */
public final class DsonExtInt32 extends DsonValue implements Comparable<DsonExtInt32> {

    private final ExtInt32 extInt32;

    public DsonExtInt32(int type, int value) {
        this(new ExtInt32(type, value));
    }

    public DsonExtInt32(int type, Integer value) {
        this(new ExtInt32(type, value));
    }

    public DsonExtInt32(int type, int value, boolean hasValue) {
        this(new ExtInt32(type, value, hasValue));
    }

    public DsonExtInt32(ExtInt32 extInt32) {
        Objects.requireNonNull(extInt32);
        this.extInt32 = extInt32;
    }

    public ExtInt32 extInt32() {
        return extInt32;
    }

    @Nonnull
    @Override
    public DsonType getDsonType() {
        return DsonType.EXT_INT32;
    }

    public int getType() {
        return extInt32.getType();
    }

    public int getValue() {
        return extInt32.getValue();
    }

    public boolean hasValue() {
        return extInt32.hasValue();
    }

    // region equals

    @Override
    public int compareTo(DsonExtInt32 o) {
        return extInt32.compareTo(o.extInt32);
    }

    @Override
    public boolean equals(Object o) {
        if (this == o) return true;
        if (o == null || getClass() != o.getClass()) return false;

        DsonExtInt32 that = (DsonExtInt32) o;

        return extInt32.equals(that.extInt32);
    }

    @Override
    public int hashCode() {
        return extInt32.hashCode();
    }

    // endregion

    @Override
    public String toString() {
        return "DsonExtInt32{" +
                "extInt32=" + extInt32 +
                '}';
    }
}
