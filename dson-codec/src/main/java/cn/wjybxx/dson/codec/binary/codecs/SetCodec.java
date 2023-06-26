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
import cn.wjybxx.dson.codec.TypeArgInfo;
import cn.wjybxx.dson.codec.binary.BinaryObjectReader;
import cn.wjybxx.dson.codec.binary.BinaryObjectWriter;
import cn.wjybxx.dson.codec.binary.BinaryPojoCodecImpl;
import cn.wjybxx.dson.codec.binary.BinaryPojoCodecScanIgnore;

import javax.annotation.Nonnull;
import java.util.LinkedHashSet;
import java.util.Set;

/**
 * @author wjybxx
 * date 2023/4/4
 */
@SuppressWarnings("rawtypes")
@BinaryPojoCodecScanIgnore
public class SetCodec implements BinaryPojoCodecImpl<Set> {

    @Nonnull
    @Override
    public Class<Set> getEncoderClass() {
        return Set.class;
    }

    @Override
    public void writeObject(Set instance, BinaryObjectWriter writer, TypeArgInfo<?> typeArgInfo) {
        TypeArgInfo<?> componentArgInfo = TypeArgInfo.of(typeArgInfo.typeArg1);
        for (Object e : instance) {
            writer.writeObject(0, e, componentArgInfo);
        }
    }

    @Override
    public Set<?> readObject(BinaryObjectReader reader, TypeArgInfo<?> typeArgInfo) {
        Set<Object> result;
        if (typeArgInfo.factory != null) {
            result = (Set<Object>) typeArgInfo.factory.get();
        } else {
            result = new LinkedHashSet<>();
        }

        TypeArgInfo<?> componentArgInfo = TypeArgInfo.of(typeArgInfo.typeArg1);
        while (reader.readDsonType() != DsonType.END_OF_OBJECT) {
            result.add(reader.readObject(0, componentArgInfo));
        }
        return result;
    }
}