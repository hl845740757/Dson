package cn.wjybxx.dson;

import cn.wjybxx.dson.types.OffsetTimestamp;
import org.junit.jupiter.api.Assertions;
import org.junit.jupiter.api.Test;

/**
 * @author wjybxx
 * date - 2023/6/28
 */
public class DsonTimeTest {

    private static final String dsonString = """
            - [
            -   @dt 2023-06-17T18:37:00,
            -   {@dt date: 2023-06-17, time: 18:37:00},
            -   {@dt date: 2023-06-17, time: 18:37:00, offset: +8},
            -   {@dt date: 2023-06-17, time: 18:37:00, offset: +08:00, millis: 100},
            -   {@dt date: 2023-06-17, time: 18:37:00, offset: +08:00, nanos: 100_000_000},
            - ]
            """;

    @Test
    void test() {
        DsonArray<String> dsonArray = Dsons.fromDson(dsonString).asArray();
        Assertions.assertEquals(dsonArray.get(0), dsonArray.get(1));

        OffsetTimestamp second = dsonArray.get(2).asTimestamp();
        OffsetTimestamp third = dsonArray.get(3).asTimestamp();
        OffsetTimestamp fourth = dsonArray.get(4).asTimestamp();
        // 偏移相同
        Assertions.assertEquals(second.getOffset(), third.getOffset());
        Assertions.assertEquals(second.getOffset(), fourth.getOffset());
        // 纳秒部分相同
        Assertions.assertEquals(third.getNanos(), fourth.getNanos());
    }
}