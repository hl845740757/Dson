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

import cn.wjybxx.dson.text.DsonMode;
import cn.wjybxx.dson.text.ObjectStyle;
import cn.wjybxx.dson.types.ObjectRef;
import org.junit.jupiter.api.Assertions;
import org.junit.jupiter.api.Test;

/**
 * @author houlei
 * date - 2023/12/27
 */
public class DsonArrayHeaderTest {

    /**
     * 测试数组首个元素有标签的情况下能否正确解析；
     * object由于会打印key，因此一定不会出现冲突
     */
    @Test
    void testHeaderInt32() {
        DsonArray<String> array = new DsonArray<>();
        array.add(new DsonInt32(64)); // 会打印类型
        array.add(new DsonInt32(64));

        String dsonString = Dsons.toDson(array, ObjectStyle.FLOW, DsonMode.RELAXED);
        System.out.println(dsonString);

        DsonValue copiedArray = Dsons.fromDson(dsonString, DsonMode.RELAXED);
        Assertions.assertEquals(array, copiedArray);
    }

    @Test
    void testHeaderRef() {
        DsonArray<String> array = new DsonArray<>();
        array.add(new DsonReference(new ObjectRef("10001"))); // 会打印类型
        array.add(new DsonInt32(64));

        String dsonString = Dsons.toDson(array, ObjectStyle.FLOW, DsonMode.RELAXED);
        System.out.println(dsonString);

        DsonValue copiedArray = Dsons.fromDson(dsonString, DsonMode.RELAXED);
        Assertions.assertEquals(array, copiedArray);
    }

    @Test
    void testHeader() {
        DsonArray<String> array = new DsonArray<>();
        array.add(new DsonReference(new ObjectRef("10001"))); // 会打印类型
        array.add(new DsonInt32(64));
        array.getHeader().append(DsonHeader.NAMES_CLASS_NAME, new DsonString("MyArray"));

        String dsonString = Dsons.toDson(array, ObjectStyle.FLOW, DsonMode.RELAXED);
        System.out.println(dsonString);

        DsonValue copiedArray = Dsons.fromDson(dsonString, DsonMode.RELAXED);
        Assertions.assertEquals(array, copiedArray);
    }
}
