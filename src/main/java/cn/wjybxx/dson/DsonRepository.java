package cn.wjybxx.dson;

import cn.wjybxx.dson.internal.CollectionUtils;
import cn.wjybxx.dson.text.DsonTextReader;
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

        String localId = getLocalId(value);
        if (localId != null) {
            DsonValue exist = indexMap.put(localId, value);
            if (exist != null) {
                CollectionUtils.removeRef(valueList, exist);
            }
        }
        return this;
    }

    public DsonValue remove(int idx) {
        DsonValue dsonValue = valueList.remove(idx);
        String localId = getLocalId(dsonValue);
        if (localId != null) {
            indexMap.remove(localId);
        }
        return dsonValue;
    }

    public boolean remove(DsonValue dsonValue) {
        int idx = CollectionUtils.indexOfRef(valueList, dsonValue);
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
            CollectionUtils.removeRef(valueList, exist);
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
        if (dsonValue instanceof DsonObject<?> dsonObject) {
            for (Map.Entry<?, DsonValue> entry : dsonObject.entrySet()) {
                DsonValue value = entry.getValue();
                if (value.getDsonType() == DsonType.REFERENCE) {
                    ObjectRef objectRef = value.asReference().getValue();
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
                    ObjectRef objectRef = value.asReference().getValue();
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

    private static String getLocalId(DsonValue dsonValue) {
        DsonHeader<?> header;
        if (dsonValue instanceof DsonObject<?> dsonObject) {
            header = dsonObject.getHeader();
        } else if (dsonValue instanceof DsonArray<?> dsonArray) {
            header = dsonArray.getHeader();
        } else {
            return null;
        }
        DsonValue wrapped = header.get(DsonHeader.NAMES_LOCAL_ID);
        return wrapped instanceof DsonString dsonString ? dsonString.getValue() : null;
    }

    public static DsonRepository fromDson(String dsonString) {
        return fromDson(dsonString, false);
    }

    public static DsonRepository fromDson(String dsonString, boolean resolveRef) {
        DsonRepository repository = new DsonRepository();
        try (DsonTextReader reader = new DsonTextReader(16, dsonString)) {
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

}