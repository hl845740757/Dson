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
            -   {@dt date: 2023-06-17, time: 18:37:00},
            -   {@dt date: 2023-06-17, time: 18:37:00, offset: +8},
            -   {@dt date: 2023-06-17, time: 18:37:00, offset: +08:00, millis: 100},
            -   {@dt date: 2023-06-17, time: 18:37:00, offset: +08:00, nanos: 100_000_000},
            - ]
            """;

    @Test
    void test() {
        DsonArray<String> dsonArray = Dsons.fromDson(dsonString).asArray();
        OffsetTimestamp second = dsonArray.get(1).asTimestamp();
        OffsetTimestamp third = dsonArray.get(2).asTimestamp();
        OffsetTimestamp fourth = dsonArray.get(3).asTimestamp();
        Assertions.assertEquals(second.getOffset(), third.getOffset());
        Assertions.assertEquals(second.getOffset(), fourth.getOffset());
        // 纳秒部分相同
        Assertions.assertEquals(third.getNanos(), fourth.getNanos());
    }
}