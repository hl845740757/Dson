package cn.wjybxx.dson;

import cn.wjybxx.dson.internal.DsonInternals;
import cn.wjybxx.dson.types.ObjectRef;

import java.util.HashMap;
import java.util.Map;
import java.util.Objects;

/**
 * 简单的Dson对象仓库实现 -- 提供简单的引用解析功能。
 *
 * @author wjybxx
 * date - 2023/6/21
 */
public class DsonRepository {

    private final Map<String, DsonValue> indexMap = new HashMap<>();
    private final DsonArray<String> container;

    public DsonRepository() {
        container = new DsonArray<>();
    }

    public DsonRepository(DsonArray<String> container) {
        this.container = Objects.requireNonNull(container);
        for (DsonValue dsonValue : container) {
            String localId = Dsons.getLocalId(dsonValue);
            if (localId != null) {
                indexMap.put(localId, dsonValue);
            }
        }
    }

    /** 勿修改返回的对象 */
    public Map<String, DsonValue> getIndexMap() {
        return indexMap;
    }

    /** 勿修改返回的对象 */
    public DsonArray<String> getContainer() {
        return container;
    }

    public int size() {
        return container.size();
    }

    public DsonValue get(int idx) {
        return container.get(idx);
    }

    public DsonRepository add(DsonValue value) {
        if (!value.getDsonType().isContainerOrHeader()) {
            throw new IllegalArgumentException();
        }
        container.add(value);

        String localId = Dsons.getLocalId(value);
        if (localId != null) {
            DsonValue exist = indexMap.put(localId, value);
            if (exist != null) {
                DsonInternals.removeRef(container, exist);
            }
        }
        return this;
    }

    public DsonValue removeAt(int idx) {
        DsonValue dsonValue = container.remove(idx);
        String localId = Dsons.getLocalId(dsonValue);
        if (localId != null) {
            indexMap.remove(localId);
        }
        return dsonValue;
    }

    public boolean remove(DsonValue dsonValue) {
        int idx = DsonInternals.indexOfRef(container, dsonValue);
        if (idx >= 0) {
            removeAt(idx);
            return true;
        } else {
            return false;
        }
    }

    public DsonValue removeById(String localId) {
        Objects.requireNonNull(localId);
        DsonValue exist = indexMap.remove(localId);
        if (exist != null) {
            DsonInternals.removeRef(container, exist);
        }
        return exist;
    }

    public DsonValue find(String localId) {
        Objects.requireNonNull(localId);
        return indexMap.get(localId);
    }

    public DsonRepository resolveReference() {
        for (DsonValue dsonValue : container) {
            resolveReference(dsonValue);
        }
        return this;
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
    public static DsonRepository fromDson(DsonReader reader) {
        return fromDson(reader, false);
    }

    public static DsonRepository fromDson(DsonReader reader, boolean resolveRef) {
        try (reader) {
            DsonRepository repository = new DsonRepository(Dsons.readTopContainer(reader));
            if (resolveRef) {
                repository.resolveReference();
            }
            return repository;
        }
    }

    // 解析引用后可能导致循环，因此equals等不实现
    @Override
    public String toString() {
        // 解析引用后可能导致死循环，因此不输出
        return "DsonRepository:" + super.toString();
    }

}