package cn.wjybxx.dson;

import org.junit.jupiter.api.Assertions;
import org.junit.jupiter.api.Test;

/**
 * @author wjybxx
 * date - 2023/6/28
 */
public class DsonTimeTest {

    private static final String dsonString = """
            - [
            -   {@dt date: 2023-06-17, time: 18:37:00},
            -   {@dt date: 2023-06-17, time: 18:37:00, offset: +8},
            -   {@dt date: 2023-06-17, time: 18:37:00, offset: +08:00, millis: 100},
            -   {@dt date: 2023-06-17, time: 18:37:00, offset: +08:00, nanos: 100_000_000},
            - ]
            """;

    @Test
    void test() {
        DsonArray<String> dsonArray = Dsons.fromDson(dsonString).asArray();
        DsonTimestamp second = dsonArray.get(1).asTimestamp();
        DsonTimestamp third = dsonArray.get(2).asTimestamp();
        DsonTimestamp fourth = dsonArray.get(3).asTimestamp();
        Assertions.assertEquals(second.getValue().getOffset(), third.getValue().getOffset());
        Assertions.assertEquals(second.getValue().getOffset(), fourth.getValue().getOffset());
        // 纳秒部分相同
        Assertions.assertEquals(third.getValue().getNanos(), fourth.getValue().getNanos());
    }
}