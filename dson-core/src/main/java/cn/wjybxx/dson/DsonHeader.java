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

import cn.wjybxx.dson.internal.InternalUtils;

import javax.annotation.Nonnull;
import java.util.LinkedHashMap;
import java.util.Map;

/**
 * 1.Header不可以再持有header，否则陷入死循环
 * 2.Header的结构应该是简单清晰的，可简单编解码
 *
 * @author wjybxx
 * date - 2023/5/27
 */
public class DsonHeader<K> extends AbstractDsonObject<K> {

    public DsonHeader() {
        super(new LinkedHashMap<>(4), InternalUtils.POLICY_DEFAULT);
    }

    public DsonHeader(Map<K, DsonValue> valueMap) {
        super(valueMap, InternalUtils.POLICY_COPY);
    }

    private DsonHeader(Map<K, DsonValue> valueMap, int policy) {
        super(valueMap, policy);
    }

    public static <K> DsonHeader<K> toImmutable(DsonHeader<K> src) {
        return new DsonHeader<>(src.valueMap, InternalUtils.POLICY_IMMUTABLE);
    }

    public static <K> DsonHeader<K> empty() {
        return new DsonHeader<>(Map.of(), InternalUtils.POLICY_IMMUTABLE);
    }

    //

    /** @return this */
    public DsonHeader<K> append(K key, DsonValue value) {
        put(key, value);
        return this;
    }

    @Nonnull
    @Override
    public final DsonType getDsonType() {
        return DsonType.HEADER;
    }

    //

    // header常见属性名
    public static final String NAMES_CLASS_NAME = "clsName";
    public static final String NAMES_COMP_CLASS_NAME = "compClsName";
    public static final String NAMES_CLASS_ID = "clsId";
    public static final String NAMES_COMP_CLASS_ID = "compClsId";
    public static final String NAMES_GUID = "guid";
    public static final String NAMES_LOCAL_ID = "localId";
    public static final String NAMES_TAGS = "tags";

    public static final int NUMBERS_CLASS_NAME = DsonLites.makeFullNumberZeroIdep(0);
    public static final int NUMBERS_COMP_CLASS_NAME = DsonLites.makeFullNumberZeroIdep(1);
    public static final int NUMBERS_CLASS_ID = DsonLites.makeFullNumberZeroIdep(2);
    public static final int NUMBERS_COMP_CLASS_ID = DsonLites.makeFullNumberZeroIdep(3);
    public static final int NUMBERS_GUID = DsonLites.makeFullNumberZeroIdep(4);
    public static final int NUMBERS_LOCAL_ID = DsonLites.makeFullNumberZeroIdep(5);
    public static final int NUMBERS_TAGS = DsonLites.makeFullNumberZeroIdep(6);

}