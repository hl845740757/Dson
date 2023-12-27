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
            - {@{MyStruct}
            - \tname : wjybxx,
            - \tage:28,
            - \t介绍: 这是一段中文而且非常非常非常非常非常非常非常非常非常非常非常非常非常非常非常非常非常非常非常非常非常非常非常非常非常非常非常非常长 ,
            - \tintro: "hello world",
            - \tref1 : {@ref localId: 10001, ns: 16148b3b4e7b8923d398},
            - \tref2 : @ref 17630eb4f916148b,
            - \tbin : [@bin 0, 35df2e75e6a4be9e6f4571c64cb6d08b0d6bc46c1754f6e9eb4a6e57e2fd53],
            - }
            -
            - {@{MyStruct}
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
    void test_equivalenceOfAllReaders() {
        DsonArray<String> topContainer1 = Dsons.fromFlatDson(dsonString);
        String dsonString1 = Dsons.toFlatDson(topContainer1);
//        System.out.println(dsonString1);

        // Binary
        {
            byte[] buffer = new byte[8192];
            DsonOutput output = DsonOutputs.newInstance(buffer);
            try (DsonWriter writer = new DsonBinaryWriter(DsonTextWriterSettings.DEFAULT, output)) {
                Dsons.writeTopContainer(writer, topContainer1);
            }
            DsonInput input = DsonInputs.newInstance(buffer, 0, output.getPosition());
            try (DsonReader reader = new DsonBinaryReader(DsonTextReaderSettings.DEFAULT, input)) {
                DsonArray<String> topContainer2 = Dsons.readTopContainer(reader);

                String dsonString2 = Dsons.toFlatDson(topContainer2);
                Assertions.assertEquals(dsonString1, dsonString2, "BinaryReader/BinaryWriter");
            }
        }
        // Object
        {
            DsonArray<String> outList = new DsonArray<>();
            try (DsonWriter writer = new DsonObjectWriter(DsonTextWriterSettings.DEFAULT, outList)) {
                Dsons.writeTopContainer(writer, topContainer1);
            }
            try (DsonReader reader = new DsonObjectReader(DsonTextReaderSettings.DEFAULT, outList)) {
                DsonArray<String> topContainer3 = Dsons.readTopContainer(reader);

                String dsonString3 = Dsons.toFlatDson(topContainer3);
                Assertions.assertEquals(dsonString1, dsonString3, "ObjectReader/ObjectWriter");
            }
        }
    }

    @Test
    void testRef() {
        DsonRepository repository = DsonRepository
                .fromDson(new DsonTextReader(DsonTextReaderSettings.DEFAULT, dsonString))
                .resolveReference();
        Assertions.assertInstanceOf(DsonArray.class, repository.find("10001"));
        Assertions.assertInstanceOf(DsonArray.class, repository.find("17630eb4f916148b"));
    }

}