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
import cn.wjybxx.dson.text.DsonTextReader;
import cn.wjybxx.dson.text.DsonTexts;
import org.junit.jupiter.api.Assertions;
import org.junit.jupiter.api.Test;

import java.util.List;
import java.util.stream.Collectors;

/**
 * 验证{@link DsonBuffer#newJsonBuffer(String)}的正确性
 *
 * @author wjybxx
 * date - 2023/6/5
 */
public class Json2DsonTest {

    private static final String jsonString = """
            {
                "Name":"C语言中文网",
                "Url":"http://c.biancheng.net/",
                "Tutorial":"JSON",
                "Article":[
                    "JSON 是什么？",
                    "JSONP 是什么？",
                    "JSON 语法规则"
                ]
            }
            """;

    @Test
    void test() {
        DsonValue dsonValue;
        try (DsonTextReader reader = new DsonTextReader(16, new DsonScanner(createLinesBuffer(jsonString)))) {
            dsonValue = Dsons.readTopDsonValue(reader);
            Assertions.assertInstanceOf(DsonObject.class, dsonValue);
        }

        DsonValue dsonValue2;
        try (DsonTextReader reader = new DsonTextReader(16, new DsonScanner(DsonBuffer.newJsonBuffer(jsonString)))) {
            dsonValue2 = Dsons.readTopDsonValue(reader);
            Assertions.assertInstanceOf(DsonObject.class, dsonValue);
        }
        Assertions.assertEquals(dsonValue, dsonValue2);
    }

    /** 用于验证{@code JsonBuffer}的正确性 */
    private static DsonBuffer createLinesBuffer(String json) {
        List<String> lines = json.lines()
                .map(e -> DsonTexts.LHEAD_APPEND + " " + e)
                .collect(Collectors.toList());
        return DsonBuffer.newLinesBuffer(lines);
    }

}