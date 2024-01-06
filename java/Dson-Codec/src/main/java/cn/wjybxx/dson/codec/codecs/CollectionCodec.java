/*
 * Copyright 2023-2024 wjybxx(845740757@qq.com)
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

package cn.wjybxx.dson.codec.codecs;

import cn.wjybxx.dson.DsonType;
import cn.wjybxx.dson.codec.DuplexCodec;
import cn.wjybxx.dson.codec.TypeArgInfo;
import cn.wjybxx.dson.codec.dson.DsonCodecScanIgnore;
import cn.wjybxx.dson.codec.dson.DsonObjectReader;
import cn.wjybxx.dson.codec.dson.DsonObjectWriter;
import cn.wjybxx.dson.codec.dsonlite.DsonLiteCodecScanIgnore;
import cn.wjybxx.dson.codec.dsonlite.DsonLiteObjectReader;
import cn.wjybxx.dson.codec.dsonlite.DsonLiteObjectWriter;
import cn.wjybxx.dson.text.ObjectStyle;

import javax.annotation.Nonnull;
import java.util.*;
import java.util.function.Supplier;

/**
 * @author wjybxx
 * date 2023/4/4
 */
@SuppressWarnings("rawtypes")
@DsonLiteCodecScanIgnore
@DsonCodecScanIgnore
public class CollectionCodec<T extends Collection> implements DuplexCodec<T> {

    final Class<T> clazz;
    final Supplier<? extends T> factory;

    public CollectionCodec(Class<T> clazz, Supplier<? extends T> factory) {
        this.clazz = Objects.requireNonNull(clazz);
        this.factory = factory;
    }

    @Nonnull
    @Override
    public Class<T> getEncoderClass() {
        return clazz;
    }

    @SuppressWarnings("unchecked")
    private Collection<Object> newCollection(TypeArgInfo<?> typeArgInfo) {
        if (typeArgInfo.factory != null) {
            return (Collection<Object>) typeArgInfo.factory.get();
        }
        if (factory != null) {
            return (Collection<Object>) factory.get();
        }
        if (Set.class.isAssignableFrom(typeArgInfo.declaredType)) {
            return new LinkedHashSet<>();
        }
        return new ArrayList<>();
    }

    @Override
    public void writeObject(DsonLiteObjectWriter writer, T instance, TypeArgInfo<?> typeArgInfo) {
        TypeArgInfo<?> componentArgInfo = TypeArgInfo.of(typeArgInfo.typeArg1);
        for (Object e : instance) {
            writer.writeObject(0, e, componentArgInfo);
        }
    }

    @Override
    public T readObject(DsonLiteObjectReader reader, TypeArgInfo<?> typeArgInfo) {
        Collection<Object> result = newCollection(typeArgInfo);
        TypeArgInfo<?> componentArgInfo = TypeArgInfo.of(typeArgInfo.typeArg1);
        while (reader.readDsonType() != DsonType.END_OF_OBJECT) {
            result.add(reader.readObject(0, componentArgInfo));
        }
        return clazz.cast(result);
    }

    @Override
    public void writeObject(DsonObjectWriter writer, T instance, TypeArgInfo<?> typeArgInfo, ObjectStyle style) {
        TypeArgInfo<?> componentArgInfo = TypeArgInfo.of(typeArgInfo.typeArg1);
        for (Object e : instance) {
            writer.writeObject(null, e, componentArgInfo, null);
        }
    }

    @Override
    public T readObject(DsonObjectReader reader, TypeArgInfo<?> typeArgInfo) {
        Collection<Object> result = newCollection(typeArgInfo);
        TypeArgInfo<?> componentArgInfo = TypeArgInfo.of(typeArgInfo.typeArg1);
        while (reader.readDsonType() != DsonType.END_OF_OBJECT) {
            result.add(reader.readObject(null, componentArgInfo));
        }
        return clazz.cast(result);
    }

}