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

/**
 * long值的简单扩展
 *
 * @author wjybxx
 * date - 2023/4/19
 */
public class ExtInt32 implements Comparable<ExtInt32> {

    private final int type;
    private final int value;

    public ExtInt32(int type, int value) {
        if (type < 0) {
            throw new IllegalArgumentException("type cant be negative");
        }
        this.type = type;
        this.value = value;
    }

    public int getType() {
        return type;
    }

    public int getValue() {
        return value;
    }

    //
    @Override
    public int compareTo(ExtInt32 that) {
        int r = Integer.compare(type, that.type);
        if (r != 0) {
            return r;
        }
        return Integer.compare(value, that.value);
    }

    @Override
    public boolean equals(Object o) {
        if (this == o) return true;
        if (o == null || getClass() != o.getClass()) return false;

        ExtInt32 that = (ExtInt32) o;

        if (type != that.type) return false;
        return value == that.value;
    }

    @Override
    public int hashCode() {
        int result = type;
        result = 31 * result + value;
        return result;
    }

    @Override
    public String toString() {
        return "ExtInt32{" +
                "type=" + type +
                ", value=" + value +
                '}';
    }
}