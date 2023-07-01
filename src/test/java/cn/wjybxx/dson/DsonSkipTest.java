package cn.wjybxx.dson;

import cn.wjybxx.dson.text.DsonTextReader;
import org.junit.jupiter.api.Assertions;
import org.junit.jupiter.api.Test;

/**
 * @author wjybxx
 * date - 2023/7/1
 */
public class DsonSkipTest {

    @Test
    void test() {
        try (DsonTextReader reader = new DsonTextReader(16, DsonTextReaderTest2.dsonString)) {
            reader.readStartObject();
            reader.skipToEndOfObject();
            reader.readEndObject();
            Assertions.assertSame(reader.readDsonType(), DsonType.END_OF_OBJECT);
        }

        try (DsonTextReader reader = new DsonTextReader(16, DsonTextReaderTest2.dsonString)) {
            reader.readStartObject();
            while (reader.readDsonType() != DsonType.END_OF_OBJECT) {
                reader.skipName();
                reader.skipValue();
            }
            reader.readEndObject();
            Assertions.assertSame(reader.readDsonType(), DsonType.END_OF_OBJECT);
        }

        // 当读取到一个嵌套对象时，跳过整个对象
        try (DsonTextReader reader = new DsonTextReader(16, DsonTextReaderTest2.dsonString)) {
            reader.readStartObject();
            while (reader.readDsonType() != DsonType.END_OF_OBJECT) {
                if (reader.isAtName()) {
                    String name = reader.readName();
                    if (name.equals("pos")) {
                        reader.skipToEndOfObject();
                        break;
                    }
                    reader.skipValue();
                } else {
                    reader.skipValue();
                }
            }
            reader.readEndObject();
            Assertions.assertSame(reader.readDsonType(), DsonType.END_OF_OBJECT);
        }
    }
}