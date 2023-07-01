package cn.wjybxx.dson;

import cn.wjybxx.dson.text.DsonTextWriterSettings;
import cn.wjybxx.dson.text.ObjectStyle;
import org.junit.jupiter.api.Assertions;
import org.junit.jupiter.api.Test;

/**
 * @author wjybxx
 * date - 2023/7/1
 */
public class DsonNumberTest {

    static final String numberString = """
            - {
            - value1: 10001,
            - value2: 1.05,
            - value3: @i 0xFF,
            - value4: @i 0b10010001,
            - value5: @i 100_000_000,
            - value6: @d 1.05E-15,
            - value7: @d Infinity,
            - value8: @d NaN,
            - }
            """;

    @Test
    void testNumber() {
        DsonValue value = Dsons.fromDson(numberString);

        String dsonString1 = Dsons.toDson(value, ObjectStyle.INDENT);
        System.out.println();
        System.out.println(dsonString1);

        String dsonString2 = Dsons.toDson(value, ObjectStyle.INDENT, DsonTextWriterSettings.newBuilder()
                .setDisableSci(true)
                .build());
        System.out.println();
        System.out.println(dsonString2);

        Assertions.assertEquals(value, Dsons.fromDson(dsonString1));
        Assertions.assertEquals(value, Dsons.fromDson(dsonString2));
    }

}