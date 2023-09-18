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

import cn.wjybxx.dson.text.DsonScanner;
import cn.wjybxx.dson.text.DsonTextReader;
import cn.wjybxx.dson.text.ObjectStyle;
import cn.wjybxx.dson.text.DsonMode;
import org.apache.commons.io.FileUtils;
import org.junit.jupiter.api.Assertions;
import org.junit.jupiter.api.Test;

import java.io.File;
import java.io.FileReader;
import java.io.IOException;
import java.nio.charset.StandardCharsets;

/**
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
        try (DsonTextReader reader = new DsonTextReader(16, Dsons.newJsonScanner(jsonString))) {
            dsonValue = Dsons.readTopDsonValue(reader);
            Assertions.assertInstanceOf(DsonObject.class, dsonValue);
        }
    }

    private static void testBigFile() throws IOException {
        String jsonFilePath = "D:\\github-mine\\Dson\\testres\\test.json";
        String dsonFilePath = "D:\\github-mine\\Dson\\testres\\testout.dson";
        try (DsonScanner scanner = Dsons.newStreamScanner(new FileReader(jsonFilePath), DsonMode.RELAXED);
             DsonTextReader reader = new DsonTextReader(16, scanner)) {
            DsonValue jsonObject = Dsons.readTopDsonValue(reader);
            DsonValue jsonObject2 = Dsons.fromJson(FileUtils.readFileToString(new File(jsonFilePath), StandardCharsets.UTF_8));
            Assertions.assertEquals(jsonObject, jsonObject2);

            String dson = Dsons.toDson(jsonObject, ObjectStyle.INDENT, true);
            DsonValue jsonObject3 = Dsons.fromJson(dson);
            Assertions.assertEquals(jsonObject, jsonObject3);

            FileUtils.writeStringToFile(new File(dsonFilePath), dson, StandardCharsets.UTF_8);
        }
    }

    public static void main(String[] args) throws IOException {
        testBigFile();
    }

}