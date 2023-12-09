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

import cn.wjybxx.dson.internal.ValuesPolicy;

import javax.annotation.Nonnull;
import java.util.LinkedHashMap;
import java.util.Map;
import java.util.Objects;

/**
 * @author wjybxx
 * date - 2023/4/21
 */
public class DsonObject<K> extends AbstractDsonObject<K> {

    private final DsonHeader<K> header;

    public DsonObject() {
        this(new LinkedHashMap<>(8), ValuesPolicy.SOURCE, new DsonHeader<>());
    }

    public DsonObject(int expectedSize) {
        this(new LinkedHashMap<>(expectedSize), ValuesPolicy.SOURCE, new DsonHeader<>());
    }

    public DsonObject(DsonObject<K> src) {
        this(src.valueMap, ValuesPolicy.COPY, new DsonHeader<>(src.getHeader()));
    }

    private DsonObject(Map<K, DsonValue> valueMap, ValuesPolicy policy, DsonHeader<K> header) {
        super(valueMap, policy);
        this.header = Objects.requireNonNull(header);
    }

    //
    private static final DsonObject<?> EMPTY = new DsonObject<>(Map.of(), ValuesPolicy.IMMUTABLE, DsonHeader.empty());

    /**
     * 创建一个禁止修改的DsonObject
     * 暂时未实现为深度的不可变，存在一些困难，主要是相互引用的问题
     */
    public static <K> DsonObject<K> toImmutable(DsonObject<K> src) {
        return new DsonObject<>(src.valueMap, ValuesPolicy.IMMUTABLE, DsonHeader.toImmutable(src.getHeader()));
    }

    @SuppressWarnings("unchecked")
    public static <K> DsonObject<K> empty() {
        return (DsonObject<K>) EMPTY;
    }

    @Nonnull
    @Override
    public final DsonType getDsonType() {
        return DsonType.OBJECT;
    }

    public DsonHeader<K> getHeader() {
        return header;
    }

    /** @return this */
    @Override
    public DsonObject<K> append(K key, DsonValue value) {
        put(key, value);
        return this;
    }

    @Override
    public String toString() {
        return "DsonObject{" +
                "header=" + header +
                ", valueMap=" + valueMap +
                '}';
    }
}