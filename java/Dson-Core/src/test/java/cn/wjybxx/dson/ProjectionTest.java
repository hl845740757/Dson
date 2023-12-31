package cn.wjybxx.dson;

import cn.wjybxx.dson.text.ObjectStyle;
import org.junit.jupiter.api.Test;

/**
 * @author wjybxx
 * date - 2023/12/31
 */
public class ProjectionTest {

    private static final String dsonString = """
            - {@{clsName:MyClassInfo, guid :10001, flags: 0}
            -   name : wjybxx,
            -   age: 28,
            -   pos :{@{Vector3} x: 1, y: 2, z: 3},
            -   address: [
            -     beijing,
            -     chengdu,
            -     shanghai
            -   ],
            -   posArr: [@{compClsName: Vector3}
            -    {x: 1, y: 1, z: 1},
            -    {x: 2, y: 2, z: 2},
            -    {x: 3, y: 3, z: 3},
            -   ],
            ~   url: @sL https://www.github.com/hl845740757
            - }
            """;

    private static final String projectInfo = """
            {
              name: 1,
              age: 1,
              pos: {
                "@": 1,
                $all: 1,
                z: 0
              },
              address: {
                $slice : [1, 2] //跳过第一个元素，然后返回两个元素
              },
              posArr: {
                "@" : 1, //返回数组的header
                $slice : 0,
                $elem: {  //投影数组元素的x和y
                  x: 1,
                  y: 1
                }
              }
            }
            """;

    @Test
    void test() {
        DsonObject<String> value = Dsons.project(dsonString, projectInfo).asObject();
        System.out.println(Dsons.toDson(value, ObjectStyle.INDENT));
    }
}