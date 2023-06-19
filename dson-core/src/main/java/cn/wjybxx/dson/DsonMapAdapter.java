package cn.wjybxx.dson;

import cn.wjybxx.dson.internal.CollectionUtils;

import java.util.*;

/**
 * @author wjybxx
 * date - 2023/6/18
 */
public abstract class DsonMapAdapter<K> extends DsonValue implements Map<K, DsonValue> {

    final Map<K, DsonValue> valueMap;

    DsonMapAdapter(Map<K, DsonValue> valueMap, int policy) {
        this.valueMap = ImmutableDsons.resolveMapPolicy(valueMap, policy);
    }

    static <K> void checkKeyValue(K key, DsonValue value) {
        if (key == null) {
            throw new IllegalArgumentException("key cant be null");
        }
        if (value == null) {
            throw new IllegalArgumentException("value cant be null");
        }
    }

    @Override
    public DsonValue put(K key, DsonValue value) {
        checkKeyValue(key, value);
        return valueMap.put(key, value);
    }

    @Override
    public void putAll(Map<? extends K, ? extends DsonValue> m) {
        // 需要检测key-value的空
        for (Map.Entry<? extends K, ? extends DsonValue> entry : m.entrySet()) {
            put(entry.getKey(), entry.getValue());
        }
    }

    public Map<K, DsonValue> getValueMap() {
        return Collections.unmodifiableMap(valueMap);
    }

    /**
     * @throws NoSuchElementException 如果对象为空
     */
    public K firstKey() {
        return CollectionUtils.firstKey(valueMap);
    }

    // region equals
    // 默认只比较valueMap

    @Override
    public boolean equals(Object o) {
        if (this == o) return true;
        return o instanceof DsonMapAdapter<?> that && valueMap.equals(that.valueMap);
    }

    @Override
    public int hashCode() {
        return valueMap.hashCode();
    }

    @Override
    public String toString() {
        return getClass().getSimpleName() + "{" +
               "valueMap=" + valueMap +
               '}';
    }
    // endregion

    // region 代理实现

    @Override
    public int size() {
        return valueMap.size();
    }

    @Override
    public boolean isEmpty() {
        return valueMap.isEmpty();
    }

    @Override
    public boolean containsKey(Object key) {
        return valueMap.containsKey(key);
    }

    @Override
    public boolean containsValue(Object value) {
        return valueMap.containsValue(value);
    }

    @Override
    public DsonValue get(Object key) {
        return valueMap.get(key);
    }

    @Override
    public DsonValue remove(Object key) {
        return valueMap.remove(key);
    }

    @Override
    public void clear() {
        valueMap.clear();
    }

    @Override
    public Set<K> keySet() {
        return valueMap.keySet();
    }

    @Override
    public Collection<DsonValue> values() {
        return valueMap.values();
    }

    @Override
    public Set<Entry<K, DsonValue>> entrySet() {
        return valueMap.entrySet();
    }

    // endregion

}