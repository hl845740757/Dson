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

import cn.wjybxx.dson.types.ExtString;

import javax.annotation.Nonnull;
import java.util.Objects;

/**
 * 字符串的简单扩展
 *
 * @author wjybxx
 * date - 2023/4/19
 */
public final class DsonExtString extends DsonValue implements Comparable<DsonExtString> {

    private final ExtString extString;

    public DsonExtString(int type, String value) {
        this(new ExtString(type, value));
    }

    public DsonExtString(ExtString extString) {
        this.extString = Objects.requireNonNull(extString);
    }

    public ExtString extString() {
        return extString;
    }

    @Nonnull
    @Override
    public DsonType getDsonType() {
        return DsonType.EXT_STRING;
    }

    public int getType() {
        return extString.getType();
    }

    public String getValue() {
        return extString.getValue();
    }

    public boolean hasValue() {
        return extString.hasValue();
    }

    //region equals

    @Override
    public int compareTo(DsonExtString that) {
        return extString.compareTo(that.extString);
    }

    @Override
    public boolean equals(Object o) {
        if (this == o) return true;
        if (o == null || getClass() != o.getClass()) return false;

        DsonExtString that = (DsonExtString) o;

        return extString.equals(that.extString);
    }

    @Override
    public int hashCode() {
        return extString.hashCode();
    }
    // endregion

    @Override
    public String toString() {
        return "DsonExtString{" +
                "extString=" + extString +
                '}';
    }
}