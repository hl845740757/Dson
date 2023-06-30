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

import javax.annotation.Nonnull;
import javax.annotation.Nullable;
import java.util.*;

import static cn.wjybxx.dson.internal.InternalUtils.nullToDef;

/**
 * @author wjybxx
 * date 2023/3/31
 */
public class CollectionUtils {

    private CollectionUtils() {

    }

    // region list扩展

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

    // region 使用“==”操作集合
    // 注意：对于拆装箱的对象慎用

    /**
     * 使用“==”判断元素是否存在
     */
    public static boolean containsRef(List<?> list, Object element) {
        for (int i = 0, size = list.size(); i < size; i++) {
            if (list.get(i) == element) {
                return true;
            }
        }
        return false;
    }

    /**
     * 使用“==”查询元素位置
     */
    public static int indexOfRef(List<?> list, Object element) {
        for (int i = 0, size = list.size(); i < size; i++) {
            if (list.get(i) == element) {
                return i;
            }
        }
        return -1;
    }

    public static int lastIndexOfRef(List<?> list, Object element) {
        for (int i = list.size() - 1; i >= 0; i--) {
            if (list.get(i) == element) {
                return i;
            }
        }
        return -1;
    }

    /**
     * 使用“==”删除对象
     */
    public static boolean removeRef(List<?> list, Object element) {
        final int index = indexOfRef(list, element);
        if (index < 0) {
            return false;
        }
        removeAt(list, index, true);
        return true;
    }

    /**
     * 使用“==”删除对象
     */
    public static boolean removeRef(List<?> list, Object element, boolean ordered) {
        final int index = indexOfRef(list, element);
        if (index < 0) {
            return false;
        }
        removeAt(list, index, ordered);
        return true;
    }
    // endregion

    @Nonnull
    public static <E> List<E> toImmutableList(@Nullable Collection<E> src) {
        return (src == null || src.isEmpty()) ? List.of() : List.copyOf(src);
    }

    /**
     * @param comparator 在转换前进行一次排序
     */
    @Nonnull
    public static <E> List<E> toImmutableList(@Nullable Collection<E> src, Comparator<? super E> comparator) {
        if (src == null || src.isEmpty()) {
            return List.of();
        }
        @SuppressWarnings("unchecked") final E[] elements = (E[]) src.toArray();
        Arrays.sort(elements, comparator);
        return List.of(elements);
    }
    // endregion

    // region set

    public static <E> HashSet<E> newHashSet(int size) {
        return new HashSet<>(capacity(size));
    }

    public static <E> LinkedHashSet<E> newLinkedHashSet(int size) {
        return new LinkedHashSet<>(capacity(size));
    }

    public static <E> Set<E> newIdentityHashSet(int size) {
        return Collections.newSetFromMap(new IdentityHashMap<>(size));
    }

    @Nonnull
    @SuppressWarnings("unchecked")
    public static <E> Set<E> toImmutableSet(@Nullable Collection<E> src) {
        if ((src == null || src.isEmpty())) {
            return Set.of();
        }
        if (src instanceof EnumSet<?> enumSet) {
            // EnumSet使用代理的方式更好，但要先拷贝保证不可变
            return (Set<E>) Collections.unmodifiableSet(enumSet.clone());
        }
        // 在Set的copy方法中会先调用new HashSet拷贝数据。
        // 我们进行一次判断并显式调用toArray可减少一次不必要的拷贝
        if (src.getClass() == HashSet.class) {
            return (Set<E>) Set.of(src.toArray());
        } else {
            return Set.copyOf(src);
        }
    }

    /** 用于需要保持元素顺序的场景 */
    public static <E> Set<E> toImmutableLinkedHashSet(@Nullable Set<E> src) {
        if (src == null || src.isEmpty()) {
            return Set.of();
        }
        return Collections.unmodifiableSet(new LinkedHashSet<>(src));
    }
    // endregion

    // region map

    /** 创建一个能存储指定元素数量的HashMap */
    public static <K, V> HashMap<K, V> newHashMap(int size) {
        return new HashMap<>(capacity(size));
    }

    /** 创建一个包含初始kv的HashMap */
    public static <K, V> HashMap<K, V> newHashMap(K k, V v) {
        HashMap<K, V> map = new HashMap<>(4);
        map.put(k, v);
        return map;
    }

    /** 创建一个能存储指定元素数量的LinkedHashMap */
    public static <K, V> LinkedHashMap<K, V> newLinkedHashMap(int size) {
        return new LinkedHashMap<>(capacity(size));
    }

    /** 创建一个包含初始kv的LinkedHashMap */
    public static <K, V> LinkedHashMap<K, V> newLinkedHashMap(K k, V v) {
        LinkedHashMap<K, V> map = new LinkedHashMap<>(4);
        map.put(k, v);
        return map;
    }

    public static <K, V> IdentityHashMap<K, V> newIdentityHashMap(int size) {
        return new IdentityHashMap<>(size);
    }

    /** 如果给定键不存在则抛出异常 */
    public static <K, V> V getOrThrow(Map<K, V> map, K key) {
        V v = map.get(key);
        if (v == null) {
            throw new IllegalArgumentException(String.format("key is absent, key %s", key));
        }
        return v;
    }

    public static <K, V> V getOrThrow(Map<K, V> map, K key, String desc) {
        V v = map.get(key);
        if (v == null) {
            throw new IllegalArgumentException(String.format("%s is absent, key %s", nullToDef(desc, "key"), key));
        }
        return v;
    }

    /** @throws NoSuchElementException 如果map为空 */
    public static <K> K firstKey(Map<K, ?> map) {
        // JDK的LinkedHashMap真的有点气人，都知道是有序的，还不让查询第一个Key...
        if (map instanceof SortedMap<K, ?> sortedMap) {
            return sortedMap.firstKey();
        } else {
            return map.keySet().iterator().next();
        }
    }

    @SuppressWarnings("unchecked")
    @Nonnull
    public static <K, V> Map<K, V> toImmutableMap(@Nullable Map<K, V> src) {
        if ((src == null || src.isEmpty())) {
            return Map.of();
        }
        if (src instanceof EnumMap<?, ?> enumMap) { // EnumMap使用代理的方式更好，但要先拷贝保证不可变
            return (Map<K, V>) Collections.unmodifiableMap(enumMap.clone());
        }
        return Map.copyOf(src);
    }

    /** 转换为不可变的{@link LinkedHashMap}，通常用于需要保留Key的顺序的场景 */
    public static <K, V> Map<K, V> toImmutableLinkedHashMap(@Nullable Map<K, V> src) {
        if ((src == null || src.isEmpty())) {
            return Map.of();
        }
        return Collections.unmodifiableMap(new LinkedHashMap<>(src));
    }
    // endregion

    // region 通用扩展

    public static boolean isEmpty(Map<?, ?> map) {
        return map == null || map.isEmpty();
    }

    public static boolean isEmpty(Collection<?> collection) {
        return collection == null || collection.isEmpty();
    }

    public static void clear(@Nullable Collection<?> collection) {
        if (collection != null) collection.clear();
    }

    public static void clear(@Nullable Map<?, ?> map) {
        if (map != null) map.clear();
    }

    // endregion

    // region 减少库依赖的方法

    private static final int MAX_POWER_OF_TWO = 1 << 30;

    private static int capacity(int expectedSize) {
        if (expectedSize < 0) {
            throw new IllegalArgumentException("expectedSize cant be negative, value: " + expectedSize);
        }
        if (expectedSize < 3) {
            return 4;
        }
        if (expectedSize < MAX_POWER_OF_TWO) {
            return (int) ((float) expectedSize / 0.75F + 1.0F);
        }
        return Integer.MAX_VALUE;
    }

    // endregion
}