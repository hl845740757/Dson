package cn.wjybxx.dson;

import cn.wjybxx.dson.text.DsonTextReader;
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
            while (reader.readDsonType() != DsonType.END_OF_OBJECT) {
                reader.skipName();
                reader.skipValue();
            }
            reader.readEndObject();
        }

        try (DsonTextReader reader = new DsonTextReader(16, DsonTextReaderTest2.dsonString)) {
            reader.readStartObject();
            reader.readDsonType();
            reader.skipName();
            reader.skipToEndOfObject();
            reader.readEndObject();
        }
    }
}