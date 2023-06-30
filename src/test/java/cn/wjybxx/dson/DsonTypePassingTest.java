package cn.wjybxx.dson;

import org.junit.jupiter.api.Assertions;
import org.junit.jupiter.api.Test;

/**
 * @author wjybxx
 * date - 2023/6/22
 */
public class DsonTypePassingTest {

    private static final String dsonString = """
            # 指定了数组元素的类型为 extInt32
            - [@{compClsName : ei, localId: 17630eb4f916148b}
            -  [ 1, 0xFFFA],
            -  [ 2, 10100],
            -  [ 3, 10010],
            -  [ 4, 10001],
            - ]
            """;

    @Test
    void test() {
        DsonArray<String> array = Dsons.fromDson(dsonString).asArray();
        for (DsonValue value : array) {
            Assertions.assertInstanceOf(DsonExtInt32.class, value);
        }
    }
}