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

import java.util.ArrayList;
import java.util.List;
import java.util.Objects;

/**
 * 对象头的默认结构体
 *
 * @author wjybxx
 * date - 2023/5/27
 */
public final class ObjectHeader {

    /** 对象的类型名 */
    private final String className;
    /** 数组成员/Object-Value的类型名 */
    private final String compClassName;
    /** 对象的本地id */
    private final String localId;
    /** 字符串自定义标签 */
    private final ArrayList<String> tags;

    public ObjectHeader(String className) {
        this(className, null, null);
    }

    public ObjectHeader(String className, String compClassName, String localId) {
        this.className = className;
        this.compClassName = compClassName;
        this.localId = localId;
        this.tags = new ArrayList<>(0); // 显式指定0，才可以避免默认扩容到10
    }

    public String getClassName() {
        return className;
    }

    public String getCompClassName() {
        return compClassName;
    }

    public String getLocalId() {
        return localId;
    }

    public List<String> getTags() {
        return tags;
    }

    public void addTag(String tag) {
        Objects.requireNonNull(tag);
        if (tags.isEmpty()) {
            tags.ensureCapacity(4);
        }
        tags.add(tag);
    }

    //

    @Override
    public boolean equals(Object o) {
        if (this == o) return true;
        if (o == null || getClass() != o.getClass()) return false;

        ObjectHeader that = (ObjectHeader) o;

        if (!Objects.equals(className, that.className)) return false;
        if (!Objects.equals(compClassName, that.compClassName))
            return false;
        if (!Objects.equals(localId, that.localId)) return false;
        return tags.equals(that.tags);
    }

    @Override
    public int hashCode() {
        int result = className != null ? className.hashCode() : 0;
        result = 31 * result + (compClassName != null ? compClassName.hashCode() : 0);
        result = 31 * result + (localId != null ? localId.hashCode() : 0);
        result = 31 * result + tags.hashCode();
        return result;
    }

    @Override
    public String toString() {
        return "ObjectHeader{" +
                "className='" + className + '\'' +
                ", compClassName='" + compClassName + '\'' +
                ", localId='" + localId + '\'' +
                ", tags=" + tags +
                '}';
    }
}