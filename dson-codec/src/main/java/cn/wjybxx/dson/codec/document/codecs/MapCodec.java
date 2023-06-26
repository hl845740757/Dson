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

package cn.wjybxx.dson.codec.document.codecs;

import cn.wjybxx.dson.DsonType;
import cn.wjybxx.dson.codec.TypeArgInfo;
import cn.wjybxx.dson.codec.document.DocumentObjectReader;
import cn.wjybxx.dson.codec.document.DocumentObjectWriter;
import cn.wjybxx.dson.codec.document.DocumentPojoCodecImpl;
import cn.wjybxx.dson.codec.document.DocumentPojoCodecScanIgnore;

import javax.annotation.Nonnull;
import java.util.LinkedHashMap;
import java.util.Map;
import java.util.Set;

/**
 * @author wjybxx
 * date 2023/4/4
 */
@SuppressWarnings("rawtypes")
@DocumentPojoCodecScanIgnore
public class MapCodec implements DocumentPojoCodecImpl<Map> {

    @Nonnull
    @Override
    public String getTypeName() {
        return "Map";
    }

    @Nonnull
    @Override
    public Class<Map> getEncoderClass() {
        return Map.class;
    }

    @Override
    public void writeObject(Map instance, DocumentObjectWriter writer, TypeArgInfo<?> typeArgInfo) {
        TypeArgInfo<?> ketArgInfo = TypeArgInfo.of(typeArgInfo.typeArg1);
        TypeArgInfo<?> valueArgInfo = TypeArgInfo.of(typeArgInfo.typeArg2);
        @SuppressWarnings("unchecked") Set<Map.Entry<?, ?>> entrySet = instance.entrySet();

        // 注意：map是一个普通的array
        for (Map.Entry<?, ?> entry : entrySet) {
            writer.writeObject(null, entry.getKey(), ketArgInfo);
            writer.writeObject(null, entry.getValue(), valueArgInfo);
        }
    }

    @Override
    public Map<?, ?> readObject(DocumentObjectReader reader, TypeArgInfo<?> typeArgInfo) {
        Map<Object, Object> result;
        if (typeArgInfo.factory != null) {
            result = (Map<Object, Object>) typeArgInfo.factory.get();
        } else {
            result = new LinkedHashMap<>();
        }

        TypeArgInfo<?> ketArgInfo = TypeArgInfo.of(typeArgInfo.typeArg1);
        TypeArgInfo<?> valueArgInfo = TypeArgInfo.of(typeArgInfo.typeArg2);
        while (reader.readDsonType() != DsonType.END_OF_OBJECT) {
            Object key = reader.readObject(null, ketArgInfo);
            Object value = reader.readObject(null, valueArgInfo);
            result.put(key, value);
        }
        return result;
    }

}