/*
 * Copyright 2023 wjybxx(845740757@qq.com)
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

package cn.wjybxx.dson;

import cn.wjybxx.dson.text.DsonBuffer;
import cn.wjybxx.dson.text.DsonScanner;
import cn.wjybxx.dson.text.DsonToken;
import cn.wjybxx.dson.text.TokenType;
import org.junit.jupiter.api.Assertions;
import org.junit.jupiter.api.Test;

import java.util.ArrayList;
import java.util.List;
import java.util.stream.Collectors;

/**
 * 验证{@link DsonBuffer#newLinesBuffer(List)}
 * 和{@link  DsonBuffer#newStringBuffer(CharSequence)}实现的相等性
 *
 * @author wjybxx
 * date - 2023/6/3
 */
public class DsonBufferTest {

    @Test
    void test() {
        String x = """
                - pos: {@Vector3 x: 0.5, y: 0.5, z: 0.5}
                -
                - posArray: [@{clsName:LinkedList,compClsName:Vector3}
                - {x: 0.1, y: 0.1, z: 0.1},
                - {x: 0.2, y: 0.2, z: 0.2}
                - ]
                -
                - {
                - k1: @i 1,
                - k2: @L 987654321,
                - k3: @f 1.05f,
                - k4: 1.0000001,
                - k5: @b true,
                - k6: @b 1,
                - k7: @N null,
                - k8: null,
                - k9: wjybxx
                - }
                - [@bin 1, FFFA]
                - [@ei 1, 10010]
                - [@eL 1, 10010]
                - [@es 1, 10010]
                -
                - @ss intro:
                |   salkjlxaaslkhalkhsal,anxksjah
                | xalsjalkjlkalhjalskhalhslahlsanlkanclxa
                | salkhaslkanlnlkhsjlanx,nalkxanla
                - lsaljsaljsalsaajsal
                - saklhskalhlsajlxlsamlkjalj
                - salkhjsaljsljldjaslna
                ~
                """;

        List<DsonToken> tokenList1 = new ArrayList<>(64);
        List<DsonToken> tokenList2 = new ArrayList<>(64);
        pullToList(new DsonScanner(DsonBuffer.newLinesBuffer(toLines(x))), tokenList1);
        pullToList(new DsonScanner(DsonBuffer.newStringBuffer(x)), tokenList2);
        Assertions.assertEquals(tokenList1, tokenList2);
    }

    private static List<String> toLines(String x) {
        return x.lines().collect(Collectors.toList());
    }

    private static void pullToList(DsonScanner scanner, List<DsonToken> outList) {
        while (true) {
            DsonToken nextToken = scanner.nextToken();
            if (nextToken.getType() == TokenType.EOF) {
                break;
            }
            outList.add(nextToken);
        }
    }
}