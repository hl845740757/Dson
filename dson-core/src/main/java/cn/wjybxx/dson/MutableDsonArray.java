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
import java.util.ArrayList;
import java.util.Objects;

/**
 * @author wjybxx
 * date - 2023/4/19
 */
public class MutableDsonArray<K> extends DsonArray<K> {

    private DsonHeader<K> header;

    public MutableDsonArray() {
        this(0, null);
    }

    public MutableDsonArray(int initCapacity) {
        this(initCapacity, new MutableDsonHeader<>());
    }

    public MutableDsonArray(int initCapacity, DsonHeader<K> header) {
        super(new ArrayList<>(initCapacity));
        this.header = Objects.requireNonNull(header);
    }

    @Nonnull
    @Override
    public DsonHeader<K> getHeader() {
        return header;
    }

    @Override
    public MutableDsonArray<K> setHeader(DsonHeader<K> header) {
        this.header = Objects.requireNonNull(header);
        return this;
    }

}