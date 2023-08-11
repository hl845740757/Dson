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

import cn.wjybxx.dson.text.DsonTextReader;
import cn.wjybxx.dson.text.DsonTextWriter;
import cn.wjybxx.dson.text.DsonTextWriterSettings;
import cn.wjybxx.dson.text.ObjectStyle;
import org.junit.jupiter.api.Assertions;
import org.junit.jupiter.api.Test;

import java.io.StringWriter;
import java.util.ArrayList;
import java.util.List;

/**
 * @author wjybxx
 * date - 2023/6/4
 */
public class DsonTextReaderTest {

    static final String dsonString = """
            - @{clsName: FileHeader, intro: 预留设计，允许定义文件头}
            -
            - {@MyStruct\s
            - \tname : wjybxx,
            - \tage:28,
            - \t介绍: 这是一段中文而且非常非常非常非常非常非常非常非常非常非常非常非常非常非常非常非常非常非常非常非常非常非常非常非常非常非常非常非常长 ,
            - \tintro: "hello world",
            - \tref1 : {@ref localId: 10001, ns: 16148b3b4e7b8923d398},
            - \tref2 : @ref 17630eb4f916148b,
            - \tbin : [@bin 0, 35df2e75e6a4be9e6f4571c64cb6d08b],
            - }
            -
            - {@MyStruct\s
            - \tname : wjybxx,
            - \tintro: "hello world",
            - \tref1 : {@ref localId: 10001, ns: 16148b3b4e7b8923d398},
            - \tref2 : @ref 17630eb4f916148b
            -  }
            -
            - [@{localId : 10001}
            -  [@bin 1, FFFA],
            -  [@ei 1, 10010],
            -  [@eL 1, 10010],
            -  [@es 1, 10010],
            -  [@es 1, null],
            - ]
            -
            - [@{compClsName : ei, localId: 17630eb4f916148b}
            -  [ 1, 0xFFFA],
            -  [ 2, 10100],
            -  [ 3, 10010],
            -  [ 4, 10001],
            - ]
            """;

    @Test
    void test() {
        List<DsonValue> topObjects = new ArrayList<>(4);
        try (DsonReader reader = new DsonTextReader(16, dsonString)) {
            DsonValue dsonValue;
            while ((dsonValue = Dsons.readTopDsonValue(reader)) != null) {
                topObjects.add(dsonValue);
            }
        }

        StringWriter stringWriter = new StringWriter();
        DsonTextWriterSettings settings = DsonTextWriterSettings.newBuilder()
                .setSoftLineLength(40)
                .setUnicodeChar(false)
                .build();

        DsonValue fileHeaderObj = topObjects.get(0);
        try (DsonTextWriter writer = new DsonTextWriter(16, stringWriter, settings)) {
            Dsons.writeTopDsonValue(writer, fileHeaderObj, ObjectStyle.INDENT);
            writer.flush();
        }
        String fileHeaderString = stringWriter.toString();
        Assertions.assertEquals(fileHeaderObj, Dsons.fromDson(fileHeaderString));
        System.out.println(fileHeaderString);
    }

    @Test
    void testRef() {
        DsonRepository repository = DsonRepository.fromDson(dsonString, true);
        Assertions.assertInstanceOf(DsonArray.class, repository.find("10001"));
        Assertions.assertInstanceOf(DsonArray.class, repository.find("17630eb4f916148b"));
    }

}