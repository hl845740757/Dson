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

import cn.wjybxx.dson.types.ObjectRef;

import javax.annotation.Nonnull;
import java.util.Objects;

/**
 * @author wjybxx
 * date - 2023/5/27
 */
public final class DsonReference extends DsonValue {

    private final ObjectRef value;

    public DsonReference(ObjectRef value) {
        this.value = Objects.requireNonNull(value);
    }

    @Nonnull
    @Override
    public DsonType getDsonType() {
        return DsonType.REFERENCE;
    }

    public ObjectRef getValue() {
        return value;
    }

    // region equals

    @Override
    public boolean equals(Object o) {
        if (this == o) return true;
        if (o == null || getClass() != o.getClass()) return false;

        DsonReference that = (DsonReference) o;

        return value.equals(that.value);
    }

    @Override
    public int hashCode() {
        return value.hashCode();
    }

    // endregion

    @Override
    public String toString() {
        return "DsonReference{" +
                "value=" + value +
                '}';
    }

}