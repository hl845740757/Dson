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
import cn.wjybxx.dson.text.DsonTextWriter;
import cn.wjybxx.dson.text.DsonTextWriterSettings;
import cn.wjybxx.dson.text.ObjectStyle;
import org.apache.commons.lang3.RandomUtils;
import org.junit.jupiter.api.Assertions;
import org.junit.jupiter.api.BeforeEach;
import org.junit.jupiter.api.Test;

import java.io.StringWriter;
import java.util.ArrayList;
import java.util.List;

/**
 * 测试编解码结果的一致性
 *
 * @author wjybxx
 * date - 2023/6/3
 */
public class DsonCodecTest {

    private static final int loop = 3;
    private List<DsonObject<String>> srcList;

    static DsonObject<String> genRandObject() {
        DsonObject<String> obj1 = new DsonObject<>(64);
        obj1.append("name", new DsonString("wjybxx"))
                .append("age", new DsonInt32(RandomUtils.nextInt(28, 32)))
                .append("url", new DsonString("http://www.wjybxx.cn"))
                .append("time", new DsonInt64(System.currentTimeMillis() + RandomUtils.nextLong(1, 1000)));


        DsonRepository repository = DsonRepository.fromDson(DsonTextReaderTest.dsonString);
        repository.remove(0); // 文件头
        obj1.append("wrapped1", repository.toDsonArray());
        obj1.append("wrapped2", Dsons.fromDson(DsonTextReaderTest2.dsonString));
        obj1.append("wrapped3", Dsons.fromDson(DsonNumberTest.numberString));
        obj1.append("wrapped4", Dsons.fromDson(DsonEscapeTest.dsonString));

        // 测试基础数字
        for (int j = 0; j < 8; j++) {
            obj1.put("iv" + j, new DsonInt32(RandomUtils.nextInt()));
            obj1.put("lv" + j, new DsonInt64(RandomUtils.nextLong()));
            obj1.put("fv" + j, new DsonFloat(RandomUtils.nextFloat()));
            obj1.put("dv" + j, new DsonDouble(RandomUtils.nextDouble()));
        }
        // 测试浮点数内联
        for (int j = 0; j < 7; j++) {
            obj1.put("ifv" + j, new DsonFloat(j + Dsons.FLOAT_INLINE_MIN));
        }
        return obj1;
    }

    @BeforeEach
    void initSrcList() {
        srcList = new ArrayList<>(loop);
        for (int i = 0; i < loop; i++) {
            DsonObject<String> obj1 = genRandObject();
            srcList.add(obj1);
        }
    }

    @Test
    void test() {
        final byte[] buffer = new byte[8192];
        int totalBytesWritten;
        try (DsonOutput dsonOutput = DsonOutputs.newInstance(buffer)) {
            DsonWriter writer = new DsonBinaryWriter(16, dsonOutput);
            for (DsonObject<String> dsonObject : srcList) {
                Dsons.writeObject(writer, dsonObject, ObjectStyle.INDENT);
            }
            totalBytesWritten = dsonOutput.getPosition();
        }
        List<DsonObject<String>> copiedList = new ArrayList<>(loop);
        try (DsonInput dsonInput = DsonInputs.newInstance(buffer, 0, totalBytesWritten)) {
            DsonReader reader = new DsonBinaryReader(16, dsonInput);
            DsonValue dsonValue;
            while ((dsonValue = Dsons.readTopDsonValue(reader)) != null) {
                copiedList.add(dsonValue.asObject());
            }
        }
        Assertions.assertEquals(srcList, copiedList);
    }

    @Test
    void testObjet() {
        DsonArray<String> dsonArray = new DsonArray<>();
        try (DsonWriter writer = new DsonObjectWriter(16, dsonArray)) {
            for (DsonObject<String> dsonObject : srcList) {
                Dsons.writeObject(writer, dsonObject, ObjectStyle.INDENT);
            }
        }
        List<DsonObject<String>> copiedList = new ArrayList<>(loop);
        try (DsonReader reader = new DsonObjectReader(16, dsonArray)) {
            DsonValue dsonValue;
            while ((dsonValue = Dsons.readTopDsonValue(reader)) != null) {
                copiedList.add(dsonValue.asObject());
            }
        }
        Assertions.assertEquals(srcList, copiedList);
    }

    @Test
    void testText() {
        DsonTextWriterSettings settings = DsonTextWriterSettings.newBuilder().build();
        StringWriter stringWriter = new StringWriter();
        try (DsonTextWriter writer = new DsonTextWriter(16, stringWriter, settings)) {
            for (DsonObject<String> dsonObject : srcList) {
                Dsons.writeObject(writer, dsonObject, ObjectStyle.INDENT);
            }
        }
        String dsonString = stringWriter.toString();
//        System.out.println(dsonString);
        List<DsonObject<String>> copiedList = new ArrayList<>(loop);
        try (DsonTextReader reader = new DsonTextReader(16, dsonString)) {
            DsonValue dsonValue;
            while ((dsonValue = Dsons.readTopDsonValue(reader)) != null) {
                copiedList.add(dsonValue.asObject());
            }
        }
        Assertions.assertEquals(srcList, copiedList);
    }

    @Test
    void testLite() {
        final byte[] buffer = new byte[4096];
        final int loop = 3;

        List<DsonObject<FieldNumber>> srcList = new ArrayList<>(loop);
        List<DsonObject<FieldNumber>> copiedList = new ArrayList<>(loop);

        int totalBytesWritten;
        try (DsonOutput dsonOutput = DsonOutputs.newInstance(buffer)) {
            DsonLiteWriter writer = new DsonBinaryLiteWriter(16, dsonOutput);
            for (int i = 0; i < loop; i++) {
                DsonObject<FieldNumber> obj1 = new DsonObject<FieldNumber>(6);
                obj1.append(FieldNumber.ofLnumber(0), new DsonString("wjybxx"))
                        .append(FieldNumber.ofLnumber(1), new DsonInt32(RandomUtils.nextInt(28, 32)))
                        .append(FieldNumber.ofLnumber(2), new DsonString("www.wjybxx.cn"))
                        .append(FieldNumber.ofLnumber(3), new DsonInt64(System.currentTimeMillis() + RandomUtils.nextLong(0, 1000)));
                srcList.add(obj1);
                DsonLites.writeObject(writer, obj1, ObjectStyle.INDENT);
            }
            totalBytesWritten = dsonOutput.getPosition();
        }

        try (DsonInput dsonInput = DsonInputs.newInstance(buffer, 0, totalBytesWritten)) {
            DsonLiteReader reader = new DsonBinaryLiteReader(16, dsonInput);
            DsonValue dsonValue;
            while ((dsonValue = DsonLites.readTopDsonValue(reader)) != null) {
                copiedList.add(dsonValue.asObjectLite());
            }
        }
        Assertions.assertEquals(srcList, copiedList);
    }

}