package cn.wjybxx.dson;

import cn.wjybxx.dson.text.DsonTextWriter;
import cn.wjybxx.dson.text.DsonTextWriterSettings;
import cn.wjybxx.dson.text.NumberStyle;
import cn.wjybxx.dson.text.ObjectStyle;
import org.junit.jupiter.api.Assertions;
import org.junit.jupiter.api.Test;

import java.io.StringWriter;
import java.util.List;

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
            - value9: @i -0xFF,
            - value10: @i -0b10010001,
            - value11: @d -1.05E-15,
            - }
            """;

    @Test
    void testNumber() {
        DsonObject<String> value = Dsons.fromDson(numberString).asObject();
        List<NumberStyle> styleList = List.of(NumberStyle.TYPED, NumberStyle.TYPED_NO_SCI,
                NumberStyle.SIGNED_HEX, NumberStyle.UNSIGNED_HEX,
                NumberStyle.SIGNED_BINARY, NumberStyle.UNSIGNED_BINARY, NumberStyle.FIXED_BINARY);

        for (NumberStyle style : styleList) {
            StringWriter stringWriter = new StringWriter(120);
            try (DsonTextWriter writer = new DsonTextWriter(DsonTextWriterSettings.newBuilder().build(), stringWriter)) {
                writer.writeStartObject(ObjectStyle.INDENT);
                if (style.supportFloat()) {
                    for (int i = 1; i <= value.size(); i++) {
                        String name = "value" + i;
                        DsonValue dsonValue = value.get(name);
                        DsonNumber dsonNumber = dsonValue.asNumber();
                        if (dsonNumber == null) {
                            break;
                        }
                        switch (dsonNumber.getDsonType()) {
                            case INT32 -> writer.writeInt32(name, dsonNumber.intValue(), WireType.VARINT, style);
                            case INT64 -> writer.writeInt64(name, dsonNumber.longValue(), WireType.VARINT, style);
                            case FLOAT -> writer.writeFloat(name, dsonNumber.floatValue(), style);
                            case DOUBLE -> writer.writeDouble(name, dsonNumber.doubleValue(), style);
                        }
                    }
                } else {
                    for (int i = 1; i <= value.size(); i++) {
                        String name = "value" + i;
                        DsonValue dsonValue = value.get(name);
                        DsonNumber dsonNumber = dsonValue.asNumber();
                        if (dsonNumber == null) {
                            break;
                        }
                        switch (dsonNumber.getDsonType()) {
                            case INT32 -> writer.writeInt32(name, dsonNumber.intValue(), WireType.VARINT, style);
                            case INT64 -> writer.writeInt64(name, dsonNumber.longValue(), WireType.VARINT, style);
                            case FLOAT -> writer.writeFloat(name, dsonNumber.floatValue(), NumberStyle.SIMPLE);
                            case DOUBLE -> writer.writeDouble(name, dsonNumber.doubleValue(), NumberStyle.SIMPLE);
                        }
                    }
                }
                writer.writeEndObject();
            }
            String dsonString1 = stringWriter.toString();
//            System.out.println(dsonString1);
            Assertions.assertEquals(value, Dsons.fromDson(dsonString1));
        }
    }

}