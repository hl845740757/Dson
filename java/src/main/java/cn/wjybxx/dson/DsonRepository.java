package cn.wjybxx.dson;

import cn.wjybxx.dson.internal.DsonInternals;
import cn.wjybxx.dson.text.DsonTextReader;
import cn.wjybxx.dson.text.DsonTextReaderSettings;
import cn.wjybxx.dson.types.ObjectRef;

import java.util.*;

/**
 * 用于简单的解析引用
 *
 * @author wjybxx
 * date - 2023/6/21
 */
public class DsonRepository {

    private final Map<String, DsonValue> indexMap = new HashMap<>();
    private final List<DsonValue> valueList = new ArrayList<>();

    public DsonRepository() {
    }

    public Map<String, DsonValue> getIndexMap() {
        return Collections.unmodifiableMap(indexMap);
    }

    public List<DsonValue> getValues() {
        return Collections.unmodifiableList(valueList);
    }

    public DsonArray<String> toDsonArray() {
        DsonArray<String> dsonArray = new DsonArray<>(valueList.size());
        dsonArray.addAll(valueList);
        return dsonArray;
    }

    public int size() {
        return valueList.size();
    }

    public DsonValue get(int idx) {
        return valueList.get(idx);
    }

    public DsonRepository add(DsonValue value) {
        if (!value.getDsonType().isContainerOrHeader()) {
            throw new IllegalArgumentException();
        }
        valueList.add(value);

        String localId = Dsons.getLocalId(value);
        if (localId != null) {
            DsonValue exist = indexMap.put(localId, value);
            if (exist != null) {
                DsonInternals.removeRef(valueList, exist);
            }
        }
        return this;
    }

    public DsonValue remove(int idx) {
        DsonValue dsonValue = valueList.remove(idx);
        String localId = Dsons.getLocalId(dsonValue);
        if (localId != null) {
            indexMap.remove(localId);
        }
        return dsonValue;
    }

    public boolean remove(DsonValue dsonValue) {
        int idx = DsonInternals.indexOfRef(valueList, dsonValue);
        if (idx >= 0) {
            remove(idx);
            return true;
        } else {
            return false;
        }
    }

    public DsonValue remove(String localId) {
        Objects.requireNonNull(localId);
        DsonValue exist = indexMap.remove(localId);
        if (exist != null) {
            DsonInternals.removeRef(valueList, exist);
        }
        return exist;
    }

    public DsonValue find(String localId) {
        Objects.requireNonNull(localId);
        return indexMap.get(localId);
    }

    public void resolveReference() {
        for (DsonValue dsonValue : valueList) {
            resolveReference(dsonValue);
        }
    }

    private void resolveReference(DsonValue dsonValue) {
        if (dsonValue instanceof AbstractDsonObject<?> dsonObject) { // 支持header...
            for (Map.Entry<?, DsonValue> entry : dsonObject.entrySet()) {
                DsonValue value = entry.getValue();
                if (value.getDsonType() == DsonType.REFERENCE) {
                    ObjectRef objectRef = value.asReference();
                    DsonValue targetObj = indexMap.get(objectRef.getLocalId());
                    if (targetObj != null) {
                        entry.setValue(targetObj);
                    }
                } else if (value.getDsonType().isContainer()) {
                    resolveReference(value);
                }
            }
        } else if (dsonValue instanceof DsonArray<?> dsonArray) {
            for (int i = 0; i < dsonArray.size(); i++) {
                DsonValue value = dsonArray.get(i);
                if (value.getDsonType() == DsonType.REFERENCE) {
                    ObjectRef objectRef = value.asReference();
                    DsonValue targetObj = indexMap.get(objectRef.getLocalId());
                    if (targetObj != null) {
                        dsonArray.set(i, targetObj);
                    }
                } else if (value.getDsonType().isContainer()) {
                    resolveReference(value);
                }
            }
        }
    }

    //

    public static DsonRepository fromDson(String dsonString) {
        return fromDson(dsonString, false);
    }

    public static DsonRepository fromDson(String dsonString, boolean resolveRef) {
        DsonRepository repository = new DsonRepository();
        try (DsonTextReader reader = new DsonTextReader(DsonTextReaderSettings.DEFAULT, dsonString)) {
            DsonValue value;
            while ((value = Dsons.readTopDsonValue(reader)) != null) {
                repository.add(value);
            }
        }
        if (resolveRef) {
            repository.resolveReference();
        }
        return repository;
    }

    @Override
    public boolean equals(Object o) {
        if (this == o) return true;
        if (o == null || getClass() != o.getClass()) return false;

        DsonRepository that = (DsonRepository) o;

        return valueList.equals(that.valueList);
    }

    @Override
    public int hashCode() {
        return valueList.hashCode();
    }

    @Override
    public String toString() {
        return "DsonRepository{" +
                "valueList=" + valueList +
                '}';
    }

}