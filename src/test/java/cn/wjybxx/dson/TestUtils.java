package cn.wjybxx.dson;

import java.util.List;
import java.util.Map;

/**
 * @author wjybxx
 * date - 2023/7/2
 */
public class TestUtils {

    @SuppressWarnings("unchecked")
    public static DsonValue toDsonValue(final Object object) {
        if (object instanceof Map<?, ?>) {
            Map<String, Object> map = (Map<String, Object>) object;
            DsonObject<String> dsonObject = new DsonObject<>();
            map.forEach((k, v) -> dsonObject.put(k, toDsonValue(v)));
            return dsonObject;
        } else if (object instanceof List<?>) {
            List<Object> array = (List<Object>) object;
            DsonArray<String> dsonArray = new DsonArray<>(array.size());
            for (Object v : array) {
                dsonArray.add(toDsonValue(v));
            }
            return dsonArray;
        } else if (object instanceof Integer i) {
            return new DsonInt32(i);
        } else if (object instanceof Long l) {
            return new DsonInt64(l);
        } else if (object instanceof Float f) {
            return new DsonFloat(f);
        } else if (object instanceof Double d) {
            return new DsonDouble(d);
        } else if (object instanceof Boolean b) {
            return DsonBool.valueOf(b);
        } else if (object instanceof String s) {
            return new DsonString(s);
        } else {
            throw new IllegalArgumentException("unsupported type " + object.getClass());
        }
    }

}