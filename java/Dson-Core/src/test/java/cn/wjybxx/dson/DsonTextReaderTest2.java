package cn.wjybxx.dson;

import cn.wjybxx.dson.text.DsonMode;
import cn.wjybxx.dson.text.DsonTextWriterSettings;
import cn.wjybxx.dson.text.ObjectStyle;
import org.junit.jupiter.api.Assertions;
import org.junit.jupiter.api.Test;

/**
 * @author wjybxx
 * date - 2023/6/15
 */
public class DsonTextReaderTest2 {

    static final String dsonString = """
            - {@{clsName:MyClassInfo, guid :10001, flags: 0}
            -   name : wjybxx,
            -   age: 28,
            -   pos :{@{Vector3} x: 0, y: 0, z: 0},
            -   address: [
            -     beijing,
            -     chengdu
            -   ],
            -   intro: @ss
            |   我是wjybxx，是一个游戏开发者，Dson是我设计的文档型数据表达法，
            | 你可以通过github联系到我。
            -   thanks
            ~   , url: @ss https://www.github.com/hl845740757
            ~   , time: {@dt date: 2023-06-17, time: 18:37:00, millis: 100, offset: +08:00}
            - }
            """;

    @Test
    void test() {
        DsonObject<String> dsonObject = Dsons.fromDson(dsonString).asObject();
        {
            String dsonString2 = Dsons.toDson(dsonObject, ObjectStyle.INDENT, DsonTextWriterSettings.newBuilder()
                    .setExtraIndent(2)
                    .setSoftLineLength(60)
                    .setTextAlignLeft(false)
                    .setTextStringLength(50)
                    .build());
            System.out.println("Mode:" + DsonMode.STANDARD + ", alignLeft:false");
            System.out.println(dsonString2);
            DsonValue dsonObject2 = Dsons.fromDson(dsonString2);
            Assertions.assertEquals(dsonObject, dsonObject2);
        }
        System.out.println();
        {
            String dsonString3 = Dsons.toDson(dsonObject, ObjectStyle.INDENT, DsonTextWriterSettings.newBuilder()
                    .setDsonMode(DsonMode.RELAXED)
                    .setExtraIndent(2)
                    .setSoftLineLength(60)
                    .build());
            System.out.println("Mode:" + DsonMode.RELAXED);
            System.out.println(dsonString3);
            DsonValue dsonObject3 = Dsons.fromJson(dsonString3);
            Assertions.assertEquals(dsonObject, dsonObject3);
        }

        // 标准模式 -- 纯文本左对齐
        {
            String dsonString2 = Dsons.toDson(dsonObject, ObjectStyle.INDENT, DsonTextWriterSettings.newBuilder()
                    .setExtraIndent(2)
                    .setSoftLineLength(50)
                    .setTextAlignLeft(true)
                    .setTextStringLength(50)
                    .build());
            System.out.println("Mode:" + DsonMode.STANDARD + ", alignLeft: true");
            System.out.println(dsonString2);
            DsonValue dsonObject2 = Dsons.fromDson(dsonString2);
            Assertions.assertEquals(dsonObject, dsonObject2);
        }
    }

}