package cn.wjybxx.dson.codec;

import cn.wjybxx.dson.internal.CollectionUtils;
import it.unimi.dsi.fastutil.ints.Int2ObjectMap;
import it.unimi.dsi.fastutil.ints.Int2ObjectOpenHashMap;

import javax.annotation.Nullable;
import java.lang.reflect.Array;
import java.util.*;
import java.util.stream.Collectors;

/**
 * 提供{@link DsonEnum}的映射实现
 *
 * @author wjybxx
 * date - 2023/6/11
 */
public class DsonEnums {

    // region 枚举映射

    /**
     * 根据枚举的values建立索引；
     *
     * @param values 枚举数组
     * @param <T>    枚举类型
     * @return unmodifiable
     */
    public static <T extends DsonEnum> DsonEnumMapper<T> mapping(final T[] values) {
        return mapping(values, false);
    }

    /**
     * 根据枚举的values建立索引；
     *
     * @param values    枚举数组
     * @param fastQuery 是否追求极致的查询性能
     * @param <T>       枚举类型
     * @return unmodifiable
     */
    @SuppressWarnings("unchecked")
    public static <T extends DsonEnum> DsonEnumMapper<T> mapping(T[] values, final boolean fastQuery) {
        if (values.length == 0) {
            return (DsonEnumMapper<T>) EmptyMapper.INSTANCE;
        }
        if (values.length == 1) {
            return new SingletonMapper<>(values[0]);
        }

        // 保护性拷贝，避免出现并发问题 - 不确定values()是否会被修改
        values = Arrays.copyOf(values, values.length);

        // 检查是否存在重复number，在拷贝之后检查才安全
        final Int2ObjectMap<T> result = new Int2ObjectOpenHashMap<>(values.length);
        for (T t : values) {
            if (result.containsKey(t.getNumber())) {
                throw new IllegalArgumentException(t.getClass().getSimpleName() + " number:" + t.getNumber() + " is duplicate");
            }
            result.put(t.getNumber(), t);
        }

        final int minNumber = minNumber(values);
        final int maxNumber = maxNumber(values);
        if (isArrayAvailable(minNumber, maxNumber, values.length, fastQuery)) {
            return new ArrayBasedMapper<>(values, minNumber, maxNumber);
        } else {
            return new MapBasedMapper<>(values, result);
        }
    }

    private static <T extends DsonEnum> int minNumber(T[] values) {
        return Arrays.stream(values)
                .mapToInt(DsonEnum::getNumber)
                .min()
                .orElseThrow();
    }

    private static <T extends DsonEnum> int maxNumber(T[] values) {
        return Arrays.stream(values)
                .mapToInt(DsonEnum::getNumber)
                .max()
                .orElseThrow();
    }

    private static boolean isArrayAvailable(int minNumber, int maxNumber, int length, boolean fastQuery) {
        if (ArrayBasedMapper.matchDefaultFactor(minNumber, maxNumber, length)) {
            return true;
        }
        if (fastQuery && ArrayBasedMapper.matchMinFactor(minNumber, maxNumber, length)) {
            return true;
        }
        return false;
    }

    private static class EmptyMapper<T extends DsonEnum> implements DsonEnumMapper<T> {

        private static final EmptyMapper<?> INSTANCE = new EmptyMapper<>();

        private EmptyMapper() {
        }

        @Nullable
        @Override
        public T forNumber(int number) {
            return null;
        }

        @Override
        public List<T> values() {
            return Collections.emptyList();
        }

        @Override
        public List<T> sortedValues() {
            return Collections.emptyList();
        }

    }

    private static class SingletonMapper<T extends DsonEnum> implements DsonEnumMapper<T> {

        private final List<T> values;

        private SingletonMapper(T val) {
            Objects.requireNonNull(val);
            values = List.of(val);
        }

        @Override
        public List<T> values() {
            return values;
        }

        @Override
        public List<T> sortedValues() {
            return values;
        }

        @Nullable
        @Override
        public T forNumber(int number) {
            final T singleton = values.get(0);
            if (number == singleton.getNumber()) {
                return singleton;
            }
            return null;
        }

        @Override
        public T checkedForNumber(int number) {
            final T singleton = values.get(0);
            if (number == singleton.getNumber()) {
                return singleton;
            }
            throw new IllegalArgumentException("No enum constant, number " + number);
        }
    }

    /**
     * 基于数组的映射，对于数量少的枚举效果好；
     * (可能存在一定空间浪费，空间换时间，如果数字基本连续，那么空间利用率很好)
     */
    private static class ArrayBasedMapper<T extends DsonEnum> implements DsonEnumMapper<T> {

        private static final float DEFAULT_FACTOR = 0.5f;
        private static final float MIN_FACTOR = 0.25f;

        private final List<T> values;
        private final List<T> sortedValues;
        private final T[] elements;

        private final int minNumber;
        private final int maxNumber;

        /**
         * @param values    枚举的所有元素
         * @param minNumber 枚举中的最小number
         * @param maxNumber 枚举中的最大number
         */
        @SuppressWarnings("unchecked")
        private ArrayBasedMapper(T[] values, int minNumber, int maxNumber) {
            this.values = List.of(values);
            this.minNumber = minNumber;
            this.maxNumber = maxNumber;

            // 数组真实长度
            final int capacity = capacity(minNumber, maxNumber);
            this.elements = (T[]) Array.newInstance(values.getClass().getComponentType(), capacity);

            // 存入数组(不一定连续)
            for (T e : values) {
                this.elements[toIndex(e.getNumber())] = e;
            }

            // 如果id是连续的，则elements就是最终的有序List
            if (capacity == values.length) {
                this.sortedValues = List.of(elements);
            } else {
                this.sortedValues = Arrays.stream(elements)
                        .filter(Objects::nonNull)
                        .collect(Collectors.toUnmodifiableList());
            }
        }

        @Nullable
        @Override
        public T forNumber(int number) {
            if (number < minNumber || number > maxNumber) {
                return null;
            }
            return elements[toIndex(number)];
        }

        @Override
        public List<T> values() {
            return values;
        }

        @Override
        public List<T> sortedValues() {
            return sortedValues;
        }

        private int toIndex(int number) {
            return number - minNumber;
        }

        private static boolean matchDefaultFactor(int minNumber, int maxNumber, int length) {
            return matchFactor(minNumber, maxNumber, length, DEFAULT_FACTOR);
        }

        private static boolean matchMinFactor(int minNumber, int maxNumber, int length) {
            return matchFactor(minNumber, maxNumber, length, MIN_FACTOR);
        }

        private static boolean matchFactor(int minNumber, int maxNumber, int length, float factor) {
            return length >= Math.ceil(capacity(minNumber, maxNumber) * factor);
        }

        private static int capacity(int minNumber, int maxNumber) {
            return maxNumber - minNumber + 1;
        }
    }

    /**
     * 基于map的映射。
     * 对于枚举值较多或数字取值范围散乱的枚举适合。
     */
    private static class MapBasedMapper<T extends DsonEnum> implements DsonEnumMapper<T> {

        private final List<T> values;
        private final List<T> sortedValues;
        private final Int2ObjectMap<T> mapping;

        private MapBasedMapper(T[] values, Int2ObjectMap<T> mapping) {
            this.values = List.of(values);
            this.mapping = mapping;

            this.sortedValues = CollectionUtils.toImmutableList(this.values, Comparator.comparingInt(DsonEnum::getNumber));
        }

        @Nullable
        @Override
        public T forNumber(int number) {
            return mapping.get(number);
        }

        @Override
        public List<T> values() {
            return values;
        }

        @Override
        public List<T> sortedValues() {
            return sortedValues;
        }

    }
    // endregion

}