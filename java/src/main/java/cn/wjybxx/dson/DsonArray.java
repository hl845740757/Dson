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
import java.util.ArrayList;
import java.util.List;
import java.util.Objects;
import java.util.RandomAccess;

/**
 * @author wjybxx
 * date - 2023/4/19
 */
public class DsonArray<K> extends AbstractDsonArray implements RandomAccess {

    private final DsonHeader<K> header;

    public DsonArray() {
        this(new ArrayList<>(), ValuesPolicy.SOURCE, new DsonHeader<>());
    }

    public DsonArray(int initCapacity) {
        this(new ArrayList<>(initCapacity), ValuesPolicy.SOURCE, new DsonHeader<>());
    }

    public DsonArray(DsonArray<K> src) {
        this(src.values, ValuesPolicy.COPY, new DsonHeader<>(src.getHeader()));
    }

    private DsonArray(List<DsonValue> values, ValuesPolicy policy, DsonHeader<K> header) {
        super(values, policy);
        this.header = Objects.requireNonNull(header);
    }

    //
    private static final DsonArray<?> EMPTY = new DsonArray<>(List.of(), ValuesPolicy.IMMUTABLE, DsonHeader.empty());

    public static <K> DsonArray<K> toImmutable(DsonArray<K> src) {
        return new DsonArray<>(src.values, ValuesPolicy.IMMUTABLE, DsonHeader.toImmutable(src.getHeader()));
    }

    @SuppressWarnings("unchecked")
    public static <K> DsonArray<K> empty() {
        return (DsonArray<K>) EMPTY;
    }

    @Nonnull
    @Override
    public final DsonType getDsonType() {
        return DsonType.ARRAY;
    }

    @Nonnull
    public DsonHeader<K> getHeader() {
        return header;
    }

    @Override
    public DsonArray<K> append(DsonValue dsonValue) {
        add(dsonValue);
        return this;
    }

}