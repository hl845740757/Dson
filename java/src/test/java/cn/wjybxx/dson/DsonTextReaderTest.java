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

import cn.wjybxx.dson.io.DsonInput;
import cn.wjybxx.dson.io.DsonInputs;
import cn.wjybxx.dson.io.DsonOutput;
import cn.wjybxx.dson.io.DsonOutputs;
import cn.wjybxx.dson.text.DsonTextReader;
import cn.wjybxx.dson.text.DsonTextReaderSettings;
import cn.wjybxx.dson.text.DsonTextWriterSettings;
import cn.wjybxx.dson.text.ObjectStyle;
import org.junit.jupiter.api.Assertions;
import org.junit.jupiter.api.Test;

/**
 * @author wjybxx
 * date - 2023/6/4
 */
public class DsonTextReaderTest {

    static final String dsonString = """
            - @{clsName: FileHeader, intro: 预留设计，允许定义文件头}
            -
            - {@MyStruct
            - \tname : wjybxx,
            - \tage:28,
            - \t介绍: 这是一段中文而且非常非常非常非常非常非常非常非常非常非常非常非常非常非常非常非常非常非常非常非常非常非常非常非常非常非常非常非常长 ,
            - \tintro: "hello world",
            - \tref1 : {@ref localId: 10001, ns: 16148b3b4e7b8923d398},
            - \tref2 : @ref 17630eb4f916148b,
            - \tbin : [@bin 0, 35df2e75e6a4be9e6f4571c64cb6d08b],
            - }
            -
            - {@MyStruct
            - \tname : wjybxx,
            - \tintro: "hello world",
            - \tref1 : {@ref localId: 10001, ns: 16148b3b4e7b8923d398},
            - \tref2 : @ref 17630eb4f916148b
            -  }
            -
            - [@{localId : 10001}
            -  [@bin 1, FFFA],
            -  [@ei 1, 10001],
            -  [@ei 2, null],
            -  [@eL 1, 20001],
            -  [@eL 2, null],
            -  [@ed 1, 0.5],
            -  [@ed 2, null],
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

    /**
     * 程序生成的无法保证和手写的文本相同
     * 但程序反复读写，以及不同方式之间的读写结果应当相同。
     */
    @Test
    void test() {
        DsonArray<String> topObjects = new DsonArray<>(6);
        try (DsonReader reader = new DsonTextReader(DsonTextReaderSettings.DEFAULT, dsonString)) {
            DsonValue dsonValue;
            while ((dsonValue = Dsons.readTopDsonValue(reader)) != null) {
                topObjects.add(dsonValue);
            }
        }
        String dsonString1 = Dsons.toDson(topObjects, ObjectStyle.INDENT);

        // Binary
        {
            byte[] buffer = new byte[8192];
            DsonOutput output = DsonOutputs.newInstance(buffer);
            try (DsonWriter writer = new DsonBinaryWriter(DsonTextWriterSettings.DEFAULT, output)) {
                for (var dsonValue : topObjects) {
                    Dsons.writeTopDsonValue(writer, dsonValue, ObjectStyle.INDENT);
                }
            }

            DsonInput input = DsonInputs.newInstance(buffer, 0, output.getPosition());
            DsonArray<String> decodedDsonArray = new DsonArray<String>();
            try (DsonReader reader = new DsonBinaryReader(DsonTextReaderSettings.DEFAULT, input)) {
                DsonValue dsonValue;
                while ((dsonValue = Dsons.readTopDsonValue(reader)) != null) {
                    decodedDsonArray.add(dsonValue);
                }
            }
            String dsonString2 = Dsons.toDson(decodedDsonArray, ObjectStyle.INDENT);
            Assertions.assertEquals(dsonString1, dsonString2, "BinaryReader/BinaryWriter");
        }
        // Object
        {
            DsonArray<String> outList = new DsonArray<>();
            try (DsonWriter writer = new DsonObjectWriter(DsonTextWriterSettings.DEFAULT, outList)) {
                for (var dsonValue : topObjects) {
                    Dsons.writeTopDsonValue(writer, dsonValue, ObjectStyle.INDENT);
                }
            }

            DsonArray<String> decodedDsonArray = new DsonArray<>();
            try (DsonReader reader = new DsonObjectReader(DsonTextReaderSettings.DEFAULT, outList)) {
                DsonValue dsonValue;
                while ((dsonValue = Dsons.readTopDsonValue(reader)) != null) {
                    decodedDsonArray.add(dsonValue);
                }
            }
            String dsonString3 = Dsons.toDson(decodedDsonArray, ObjectStyle.INDENT);
            Assertions.assertEquals(dsonString1, dsonString3, "ObjectReader/ObjectWriter");
        }
    }

    @Test
    void testRef() {
        DsonRepository repository = DsonRepository.fromDson(dsonString, true);
        Assertions.assertInstanceOf(DsonArray.class, repository.find("10001"));
        Assertions.assertInstanceOf(DsonArray.class, repository.find("17630eb4f916148b"));
    }

}