package cn.wjybxx.dson;

import cn.wjybxx.dson.text.*;
import org.junit.jupiter.api.Test;

import java.io.StringWriter;
import java.util.ArrayList;
import java.util.List;

/**
 * @author wjybxx
 * date - 2023/6/15
 */
public class DsonTextReaderTest2 {

    private static final String dsonString = """
            -- {@{className: MyClassInfo, guid: 10001, flags: 0}
            --     name: wjybxx,
            --     age: 28,
            --     pos: {@Vector3 x: 0, y: 0, z: 0},
            --     address: [
            --         beijng,
            --         chengdu,
            --     ],
            --     intro: @ss\s
            -|     我是wjybxx，是一个游戏开发者，Dson是我设计的文档型数据表达法，
            -| 你可以通过github联系到我。
            ->     thanks
            --   , url: @ss https://www.github.com/hl845740757
            -- }
            """;

    @Test
    void test() {
        List<DsonValue> topObjects = new ArrayList<>(4);
        try (DsonScanner scanner = new DsonScanner(new DsonStringBuffer(dsonString))) {
            DsonReader reader = new DsonTextReader(16, scanner);
            DsonValue dsonValue;
            while ((dsonValue = Dsons.readTopDsonValue(reader)) != null) {
                topObjects.add(dsonValue);
            }
        }

        StringWriter stringWriter = new StringWriter();
        DsonTextWriterSettings settings = DsonTextWriterSettings.newBuilder()
                .setSoftLineLength(30)
                .setUnicodeChar(false)
                .build();

        try (DsonTextWriter writer = new DsonTextWriter(16, stringWriter, settings)) {
            for (DsonValue dsonValue : topObjects) {
                Dsons.writeTopDsonValue(writer, dsonValue, ObjectStyle.INDENT);
            }
        }
        System.out.println(stringWriter.toString());
    }
}