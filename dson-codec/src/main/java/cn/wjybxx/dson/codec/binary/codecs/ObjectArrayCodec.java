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

package cn.wjybxx.dson.codec.binary.codecs;

import cn.wjybxx.dson.DsonType;
import cn.wjybxx.dson.codec.ConverterUtils;
import cn.wjybxx.dson.codec.TypeArgInfo;
import cn.wjybxx.dson.codec.binary.BinaryObjectReader;
import cn.wjybxx.dson.codec.binary.BinaryObjectWriter;
import cn.wjybxx.dson.codec.binary.BinaryPojoCodecImpl;
import cn.wjybxx.dson.codec.binary.BinaryPojoCodecScanIgnore;

import javax.annotation.Nonnull;
import java.util.ArrayList;

/**
 * @author wjybxx
 * date 2023/4/4
 */
@BinaryPojoCodecScanIgnore
public class ObjectArrayCodec implements BinaryPojoCodecImpl<Object[]> {

    @Nonnull
    @Override
    public Class<Object[]> getEncoderClass() {
        return Object[].class;
    }

    private static TypeArgInfo<?> findComponentTypeArg(Class<?> declaredType) {
        if (!declaredType.isArray()) {
            throw new IllegalArgumentException("declaredType is not arrayType, info " + declaredType);
        }
        Class<?> componentType = declaredType.getComponentType();
        if (componentType != Object.class) {
            return TypeArgInfo.of(componentType);
        }
        return TypeArgInfo.OBJECT;
    }

    @Override
    public void writeObject(Object[] instance, BinaryObjectWriter writer, TypeArgInfo<?> typeArgInfo) {
        TypeArgInfo<?> componentArgInfo = findComponentTypeArg(typeArgInfo.declaredType);
        for (Object e : instance) {
            writer.writeObject(0, e, componentArgInfo);
        }
    }

    @Override
    public Object[] readObject(BinaryObjectReader reader, TypeArgInfo<?> typeArgInfo) {
        ArrayList<Object> result = new ArrayList<>();

        TypeArgInfo<?> componentArgInfo = findComponentTypeArg(typeArgInfo.declaredType);
        while (reader.readDsonType() != DsonType.END_OF_OBJECT) {
            result.add(reader.readObject(0, componentArgInfo));
        }

        // 一定不是基础类型数组
        return (Object[]) ConverterUtils.convertList2Array(result, typeArgInfo.declaredType);
    }
}