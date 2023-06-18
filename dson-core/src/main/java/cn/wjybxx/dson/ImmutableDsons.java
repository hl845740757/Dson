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

import it.unimi.dsi.fastutil.objects.Object2ObjectArrayMap;

import java.util.*;

/**
 * @author wjybxx
 * date - 2023/5/30
 */
final class ImmutableDsons {

    static final int POLICY_DEFAULT = 0;
    static final int POLICY_COPY = 1;
    static final int POLICY_IMMUTABLE = 2;

    /** @param policy 策略：0.直接持有 1.可变拷贝 2.不可变拷贝 */
    static <K> Map<K, DsonValue> resolveMapPolicy(Map<K, DsonValue> valueMap, int policy) {
        Objects.requireNonNull(valueMap, "valueMap");
        if (policy == 1) {
            if (valueMap.getClass() == Object2ObjectArrayMap.class) {
                return new Object2ObjectArrayMap<>(valueMap);
            } else {
                return new LinkedHashMap<>(valueMap);
            }
        } else if (policy == 2) {
            // 需要保持顺序，size小的情况下使用ArrayMap
            if (valueMap.size() <= 4) {
                return Collections.unmodifiableMap(new Object2ObjectArrayMap<>(valueMap));
            } else {
                return Collections.unmodifiableMap(new LinkedHashMap<>(valueMap));
            }
        } else {
            return valueMap;
        }
    }

    /** @param policy 策略：0.直接持有 1.可变拷贝 2.不可变拷贝 */
    static List<DsonValue> resolveListPolicy(List<DsonValue> values, int policy) {
        Objects.requireNonNull(values, "values");
        if (policy == 1) {
            return new ArrayList<>(values);
        } else if (policy == 2) {
            return List.copyOf(values);
        } else {
            return values;
        }
    }
}