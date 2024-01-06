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

package cn.wjybxx.dson.codec.dsonlite;

import cn.wjybxx.dson.codec.ConverterUtils;
import cn.wjybxx.dson.codec.codecs.*;

import javax.annotation.Nullable;
import java.util.*;
import java.util.concurrent.ConcurrentHashMap;

/**
 * @author wjybxx
 * date 2023/4/1
 */
public class DsonLiteConverterUtils extends ConverterUtils {

    /** 默认codec注册表 */
    private static final DsonLiteCodecRegistry CODEC_REGISTRY;

    static {
        // 基础类型的数组codec用于避免拆装箱，提高性能
        List<DsonLiteCodecImpl<?>> entryList = List.of(
                newCodec(new IntArrayCodec()),
                newCodec(new LongArrayCodec()),
                newCodec(new FloatArrayCodec()),
                newCodec(new DoubleArrayCodec()),
                newCodec(new BooleanArrayCodec()),
                newCodec(new StringArrayCodec()),
                newCodec(new ShortArrayCodec()),
                newCodec(new CharArrayCodec()),

                newCodec(new ObjectArrayCodec()),
                newCodec(new CollectionCodec<>(Collection.class, null)),
                newCodec(new MapCodec<>(Map.class, null)),

                // 常用具体类型集合 -- 不含默认解码类型的超类
                newCodec(new CollectionCodec<>(LinkedList.class, LinkedList::new)),
                newCodec(new CollectionCodec<>(ArrayDeque.class, ArrayDeque::new)),
                newCodec(new MapCodec<>(IdentityHashMap.class, IdentityHashMap::new)),
                newCodec(new MapCodec<>(ConcurrentHashMap.class, ConcurrentHashMap::new)),

                // dson内建结构
                newCodec(new BinaryCodec()),
                newCodec(new ExtInt32Codec()),
                newCodec(new ExtInt64Codec()),
                newCodec(new ExtDoubleCodec()),
                newCodec(new ExtStringCodec())
        );

        Map<Class<?>, DsonLiteCodecImpl<?>> codecMap = DsonLiteCodecRegistries.newCodecMap(entryList);
        CODEC_REGISTRY = new DefaultCodecRegistry(codecMap);
    }


    private static <T> DsonLiteCodecImpl<T> newCodec(DsonLiteCodec<T> codecImpl) {
        return new DsonLiteCodecImpl<>(codecImpl);
    }

    public static DsonLiteCodecRegistry getDefaultCodecRegistry() {
        return CODEC_REGISTRY;
    }

    @SuppressWarnings({"rawtypes", "unchecked"})
    private static class DefaultCodecRegistry implements DsonLiteCodecRegistry {

        final Map<Class<?>, DsonLiteCodecImpl<?>> codecMap;

        final DsonLiteCodecImpl<Object[]> objectArrayCodec;
        final DsonLiteCodecImpl<Collection> collectionCodec;
        final DsonLiteCodecImpl<Map> mapCodec;

        private DefaultCodecRegistry(Map<Class<?>, DsonLiteCodecImpl<?>> codecMap) {
            this.codecMap = codecMap;

            this.objectArrayCodec = getCodec(codecMap, Object[].class);
            this.collectionCodec = getCodec(codecMap, Collection.class);
            this.mapCodec = getCodec(codecMap, Map.class);
        }

        private static <T> DsonLiteCodecImpl<T> getCodec(Map<Class<?>, DsonLiteCodecImpl<?>> codecMap, Class<T> clazz) {
            DsonLiteCodecImpl<T> codec = (DsonLiteCodecImpl<T>) codecMap.get(clazz);
            if (codec == null) throw new IllegalArgumentException(clazz.getName());
            return codec;
        }

        @Nullable
        @Override
        public <T> DsonLiteCodecImpl<T> get(Class<T> clazz) {
            DsonLiteCodecImpl<?> codec = codecMap.get(clazz);
            if (codec != null) return (DsonLiteCodecImpl<T>) codec;

            if (clazz.isArray()) return (DsonLiteCodecImpl<T>) objectArrayCodec;
            if (Collection.class.isAssignableFrom(clazz)) return (DsonLiteCodecImpl<T>) collectionCodec;
            if (Map.class.isAssignableFrom(clazz)) return (DsonLiteCodecImpl<T>) mapCodec;
            return null;
        }
    }

}