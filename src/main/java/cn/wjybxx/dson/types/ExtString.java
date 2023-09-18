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

import java.util.Objects;

/**
 * 字符串的简单扩展
 *
 * @author wjybxx
 * date - 2023/4/19
 */
public class ExtString implements Comparable<ExtString> {

    private final int type;
    private final String value;

    public ExtString(int type, String value) {
        if (type < 0) {
            throw new IllegalArgumentException("type cant be negative");
        }
        this.type = type;
        this.value = value;
    }

    public int getType() {
        return type;
    }

    public String getValue() {
        return value;
    }

    public boolean hasValue() {
        return value != null;
    }

    //
    @Override
    public int compareTo(ExtString that) {
        int r = Integer.compare(type, that.type);
        if (r != 0) {
            return r;
        }
        if (value != null && that.value != null) {
            return value.compareTo(that.value);
        }
        if (value == null) {
            return -1;
        } else {
            return 1;
        }
    }

    @Override
    public boolean equals(Object o) {
        if (this == o) return true;
        if (o == null || getClass() != o.getClass()) return false;

        ExtString that = (ExtString) o;

        if (type != that.type) return false;
        return Objects.equals(value, that.value);
    }

    @Override
    public int hashCode() {
        int result = type;
        result = 31 * result + (value != null ? value.hashCode() : 0);
        return result;
    }

    @Override
    public String toString() {
        return "ExtString{" +
                "type=" + type +
                ", value='" + value + '\'' +
                '}';
    }
}