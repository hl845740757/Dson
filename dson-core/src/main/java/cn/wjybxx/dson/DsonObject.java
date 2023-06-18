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
import java.util.LinkedHashMap;
import java.util.Map;
import java.util.Objects;

/**
 * @author wjybxx
 * date - 2023/4/21
 */
public class DsonObject<K> extends DsonMapAdapter<K> {

    private final DsonHeader<K> header;

    public DsonObject() {
        this(new LinkedHashMap<>(8), ImmutableDsons.POLICY_DEFAULT, new DsonHeader<>());
    }

    public DsonObject(int expectedSize) {
        this(new LinkedHashMap<>(expectedSize), ImmutableDsons.POLICY_DEFAULT, new DsonHeader<>());
    }

    public DsonObject(int expectedSize, DsonHeader<K> header) {
        this(new LinkedHashMap<>(expectedSize), ImmutableDsons.POLICY_DEFAULT, header);
    }

    public DsonObject(DsonObject<K> src) {
        this(src.valueMap, ImmutableDsons.POLICY_COPY, new DsonHeader<>(src.getHeader()));
    }

    private DsonObject(Map<K, DsonValue> valueMap, int policy, DsonHeader<K> header) {
        super(valueMap, policy);
        this.header = Objects.requireNonNull(header);
    }

    //
    private static final DsonObject<?> EMPTY = new DsonObject<>(Map.of(), ImmutableDsons.POLICY_IMMUTABLE,
            DsonHeader.empty());

    /**
     * 创建一个禁止修改的DsonObject
     * 暂时未实现为深度的不可变，存在一些困难，主要是相互引用的问题
     */
    public static <K> DsonObject<K> toImmutable(DsonObject<K> src) {
        return new DsonObject<>(src.valueMap, ImmutableDsons.POLICY_IMMUTABLE,
                DsonHeader.toImmutable(src.getHeader()));
    }

    @SuppressWarnings("unchecked")
    public static <K> DsonObject<K> empty() {
        return (DsonObject<K>) EMPTY;
    }

    public DsonHeader<K> getHeader() {
        return header;
    }

    /** @return this */
    public DsonObject<K> append(K key, DsonValue value) {
        put(key, value);
        return this;
    }

    @Nonnull
    @Override
    public final DsonType getDsonType() {
        return DsonType.OBJECT;
    }
}