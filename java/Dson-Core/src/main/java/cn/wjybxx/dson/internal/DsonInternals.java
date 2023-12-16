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

package cn.wjybxx.dson.internal;

import org.apache.commons.lang3.BooleanUtils;
import org.apache.commons.lang3.math.NumberUtils;

import javax.annotation.Nonnull;
import javax.annotation.Nullable;
import java.util.*;

/**
 * 存放一些基础的工具方法，不想定义过多的小类，减少维护量
 *
 * @author wjybxx
 * date - 2023/4/17
 */
public class DsonInternals {

    /**
     * 如果给定参数为null，则返回给定的默认值，否则返回值本身
     * {@link Objects#requireNonNullElse(Object, Object)}不允许def为null
     */
    public static <V> V nullToDef(V obj, V def) {
        return obj == null ? def : obj;
    }

    public static void recoveryInterrupt(Throwable ex) {
        if (ex instanceof InterruptedException) {
            Thread.currentThread().interrupt();
        }
    }

    // region properties

    public static int getInt(Properties properties, String key, int def) {
        String v = properties.getProperty(key);
        return NumberUtils.toInt(v, def);
    }

    public static long getLong(Properties properties, String key, long def) {
        String v = properties.getProperty(key);
        return NumberUtils.toLong(v, def);
    }

    public static boolean getBool(Properties properties, String key, boolean def) {
        String v = properties.getProperty(key);
        return v != null ? BooleanUtils.toBoolean(v) : def;
    }

    public static String getString(Properties properties, String key, String def) {
        return properties.getProperty(key, def);
    }

    // endregion

    // region bits

    public static boolean isEnabled(int value, int mask) {
        return (value & mask) == mask;
    }

    public static boolean isDisabled(int value, int mask) {
        return (value & mask) != mask;
    }

    // endregion

    // region collection

    /**
     * 删除指定位置的元素，可以选择是否保持列表中元素的顺序，当不需要保持顺序时可以对删除性能进行优化
     * 注意：应当小心使用该特性，能够使用该特性的场景不多，应当慎之又慎。
     *
     * @param ordered 是否保持之前的顺序。
     * @return 删除的元素
     */
    public static <E> E removeAt(List<E> list, int index, boolean ordered) {
        if (ordered) {
            return list.remove(index);
        } else {
            // 将最后一个元素赋值到要删除的位置，然后删除最后一个
            final E deleted = list.get(index);
            final int tailIndex = list.size() - 1;
            if (index < tailIndex) {
                list.set(index, list.get(tailIndex));
            }
            list.remove(tailIndex);
            return deleted;
        }
    }

    /** 使用“==”判断元素是否存在 */
    public static boolean containsRef(List<?> list, Object element) {
        for (int i = 0, size = list.size(); i < size; i++) {
            if (list.get(i) == element) {
                return true;
            }
        }
        return false;
    }

    /** 使用“==”查询元素位置 */
    public static int indexOfRef(List<?> list, Object element) {
        for (int i = 0, size = list.size(); i < size; i++) {
            if (list.get(i) == element) {
                return i;
            }
        }
        return -1;
    }

    /** 使用“==”查询元素位置 */
    public static int lastIndexOfRef(List<?> list, Object element) {
        for (int i = list.size() - 1; i >= 0; i--) {
            if (list.get(i) == element) {
                return i;
            }
        }
        return -1;
    }

    /** 使用“==”删除对象 */
    public static boolean removeRef(List<?> list, Object element) {
        final int index = indexOfRef(list, element);
        if (index < 0) {
            return false;
        }
        list.remove(index);
        return true;
    }

    /** 使用“==”删除对象 */
    public static boolean removeRef(List<?> list, Object element, boolean ordered) {
        final int index = indexOfRef(list, element);
        if (index < 0) {
            return false;
        }
        removeAt(list, index, ordered);
        return true;
    }

    @Nonnull
    public static <E> List<E> toImmutableList(@Nullable Collection<E> src) {
        return (src == null || src.isEmpty()) ? List.of() : List.copyOf(src);
    }

    /** @param comparator 在转换前进行一次排序 */
    @Nonnull
    public static <E> List<E> toImmutableList(@Nullable Collection<E> src, Comparator<? super E> comparator) {
        if (src == null || src.isEmpty()) {
            return List.of();
        }
        @SuppressWarnings("unchecked") final E[] elements = (E[]) src.toArray();
        Arrays.sort(elements, comparator);
        return List.of(elements);
    }

    /** 用于需要保持元素顺序的场景 */
    public static <E> Set<E> toImmutableLinkedHashSet(@Nullable Set<E> src) {
        if (src == null || src.isEmpty()) {
            return Set.of();
        }
        return Collections.unmodifiableSet(new LinkedHashSet<>(src));
    }

    /** 转换为不可变的{@link LinkedHashMap}，通常用于需要保留Key的顺序的场景 */
    public static <K, V> Map<K, V> toImmutableLinkedHashMap(@Nullable Map<K, V> src) {
        if ((src == null || src.isEmpty())) {
            return Map.of();
        }
        return Collections.unmodifiableMap(new LinkedHashMap<>(src));
    }

    /** @throws NoSuchElementException 如果map为空 */
    public static <K> K firstKey(Map<K, ?> map) {
        // JDK21支持查询FirstKey
        if (map instanceof SortedMap<K, ?> sortedMap) {
            return sortedMap.firstKey();
        } else {
            return map.keySet().iterator().next();
        }
    }

    // endregion

}