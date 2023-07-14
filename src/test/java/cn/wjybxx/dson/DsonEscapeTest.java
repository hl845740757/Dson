package cn.wjybxx.dson;

import cn.wjybxx.dson.text.ObjectStyle;
import org.junit.jupiter.api.Assertions;
import org.junit.jupiter.api.Test;

/**
 * @author wjybxx
 * date - 2023/6/22
 */
public class DsonEscapeTest {

    private static final String regExp = "^[\\u4e00-\\u9fa5_a-zA-Z0-9]+$";

    private static final String dsonString = """
            - {
            #   @ss 纯文本模式下输入正则表达式
            -   reg1: [@es 10, @ss ^[\\u4e00-\\u9fa5_a-zA-Z0-9]+$
            ~   ],
                        
            #   在纯文本模式插入转义版本的正则表达式
            -   reg2: [@es 10, @ss\s
            ^ ^[\\\\u4e00-\\\\u9fa5_a-zA-Z0-9]+$
            ~   ],
                    
            #   在双引号模式下插入纯文本的正则表达式，结束行需要使用 '|' 否则会插入换行符导致不等
            -   reg3: "
            ^ ^[\\u4e00-\\u9fa5_a-zA-Z0-9]+$
            | "
            - }
            """;

    @Test
    void test() {
        DsonValue value = Dsons.fromDson(dsonString);
        DsonExtString reg1 = value.asObject().get("reg1").asExtString();
        Assertions.assertEquals(regExp, reg1.getValue());

        DsonExtString reg2 = value.asObject().get("reg2").asExtString();
        Assertions.assertEquals(regExp, reg2.getValue());

        DsonString reg3 = value.asObject().get("reg3").asString();
        Assertions.assertEquals(regExp, reg3.getValue());

        System.out.println(Dsons.toDson(value, ObjectStyle.INDENT));
    }
}