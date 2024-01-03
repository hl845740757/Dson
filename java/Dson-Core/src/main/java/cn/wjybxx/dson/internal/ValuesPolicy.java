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

package cn.wjybxx.dson.internal;

import cn.wjybxx.base.CollectionUtils;

import java.util.*;

/**
 * @author wjybxx
 * date - 2023/7/25
 */
public enum ValuesPolicy {

    SOURCE {
        @Override
        public <K, V> Map<K, V> applyMap(Map<K, V> src) {
            return src;
        }

        @Override
        public <E> List<E> applyList(List<E> src) {
            return src;
        }

        @Override
        public <E> Set<E> applySet(Set<E> src) {
            return src;
        }
    },

    COPY {
        @Override
        public <K, V> Map<K, V> applyMap(Map<K, V> src) {
            return new LinkedHashMap<>(src);
        }

        @Override
        public <E> List<E> applyList(List<E> src) {
            return new ArrayList<>(src);
        }

        @Override
        public <E> Set<E> applySet(Set<E> src) {
            return new LinkedHashSet<>(src);
        }
    },

    IMMUTABLE {
        @Override
        public <K, V> Map<K, V> applyMap(Map<K, V> src) {
            return CollectionUtils.toImmutableLinkedHashMap(src);
        }

        @Override
        public <E> List<E> applyList(List<E> src) {
            return List.copyOf(src);
        }

        @Override
        public <E> Set<E> applySet(Set<E> src) {
            return CollectionUtils.toImmutableLinkedHashSet(src);
        }
    },

    ;

    public abstract <K, V> Map<K, V> applyMap(Map<K, V> src);

    public abstract <E> List<E> applyList(List<E> src);

    public abstract <E> Set<E> applySet(Set<E> src);

}