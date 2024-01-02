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

package cn.wjybxx.dson.types;

import cn.wjybxx.dson.DsonLites;
import cn.wjybxx.dson.internal.DsonInternals;

import javax.annotation.concurrent.Immutable;
import java.util.Objects;

/**
 * 对象引用的默认结构体
 *
 * @author wjybxx
 * date - 2023/5/26
 */
@Immutable
public final class ObjectRef {

    public static final int MASK_NAMESPACE = 1;
    public static final int MASK_TYPE = 1 << 1;
    public static final int MASK_POLICY = 1 << 2;

    /** 引用对象的本地id -- 如果目标对象是容器中的一员，该值是其容器内编号 */
    private final String localId;
    /** 引用对象所属的命名空间 */
    private final String namespace;
    /** 引用的对象的大类型 -- 给业务使用的，用于快速引用分析 */
    private final int type;
    /** 引用的解析策略 -- 自定义解析规则 */
    private final int policy;

    public ObjectRef(String localId) {
        this(localId, null, 0, 0);
    }

    public ObjectRef(String localId, String namespace) {
        this(localId, namespace, 0, 0);
    }

    public ObjectRef(String localId, String namespace, int type, int policy) {
        this.localId = DsonInternals.nullToDef(localId, "");
        this.namespace = DsonInternals.nullToDef(namespace, "");
        this.type = type;
        this.policy = policy;
    }

    public boolean isEmpty() {
        return DsonInternals.isBlank(namespace) && DsonInternals.isBlank(localId);
    }

    public boolean hasLocalId() {
        return !DsonInternals.isBlank(localId);
    }

    public boolean hasNamespace() {
        return !DsonInternals.isBlank(namespace);
    }

    public String getLocalId() {
        return localId;
    }

    public String getNamespace() {
        return namespace;
    }

    public int getPolicy() {
        return policy;
    }

    public int getType() {
        return type;
    }

    //region equals

    @Override
    public boolean equals(Object o) {
        if (this == o) return true;
        if (o == null || getClass() != o.getClass()) return false;

        ObjectRef objectRef = (ObjectRef) o;

        if (type != objectRef.type) return false;
        if (policy != objectRef.policy) return false;
        if (!Objects.equals(namespace, objectRef.namespace)) return false;
        return Objects.equals(localId, objectRef.localId);
    }

    @Override
    public int hashCode() {
        int result = namespace != null ? namespace.hashCode() : 0;
        result = 31 * result + (localId != null ? localId.hashCode() : 0);
        result = 31 * result + type;
        result = 31 * result + policy;
        return result;
    }
    // endregion

    @Override
    public String toString() {
        return "ObjectRef{" +
                "namespace='" + namespace + '\'' +
                ", localId='" + localId + '\'' +
                ", type=" + type +
                ", policy=" + policy +
                '}';
    }

    // ref常见属性名
    public static final String NAMES_NAMESPACE = "ns";
    public static final String NAMES_LOCAL_ID = "localId";
    public static final String NAMES_TYPE = "type";
    public static final String NAMES_POLICY = "policy";

    public static final int NUMBERS_NAMESPACE = DsonLites.makeFullNumberZeroIdep(0);
    public static final int NUMBERS_LOCAL_ID = DsonLites.makeFullNumberZeroIdep(1);
    public static final int NUMBERS_TYPE = DsonLites.makeFullNumberZeroIdep(2);
    public static final int NUMBERS_POLICY = DsonLites.makeFullNumberZeroIdep(3);
}