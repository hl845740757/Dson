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

package cn.wjybxx.dson.codec.dson;

import cn.wjybxx.dson.codec.ConverterUtils;
import cn.wjybxx.dson.codec.codecs.*;

import javax.annotation.Nullable;
import java.util.*;
import java.util.concurrent.ConcurrentHashMap;

/**
 * @author wjybxx
 * date 2023/4/4
 */
public class DsonConverterUtils extends ConverterUtils {

    public static final String NUMBER_KEY = "number";
    /** 默认codec注册表 */
    private static final DsonCodecRegistry CODEC_REGISTRY;

    static {
        List<DsonCodecImpl<?>> entryList = List.of(
                // 基础类型的数组codec用于避免拆装箱，提高性能
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

        Map<Class<?>, DsonCodecImpl<?>> codecMap = DsonCodecRegistries.newCodecMap(entryList);
        CODEC_REGISTRY = new DefaultCodecRegistry(codecMap);
    }

    private static <T> DsonCodecImpl<T> newCodec(DsonCodec<T> codecImpl) {
        return new DsonCodecImpl<>(codecImpl);
    }

    public static DsonCodecRegistry getDefaultCodecRegistry() {
        return CODEC_REGISTRY;
    }

    @SuppressWarnings({"rawtypes", "unchecked"})
    private static class DefaultCodecRegistry implements DsonCodecRegistry {

        final Map<Class<?>, DsonCodecImpl<?>> codecMap;

        final DsonCodecImpl<Object[]> objectArrayCodec;
        final DsonCodecImpl<Collection> collectionCodec;
        final DsonCodecImpl<Map> mapCodec;

        private DefaultCodecRegistry(Map<Class<?>, DsonCodecImpl<?>> codecMap) {
            this.codecMap = codecMap;

            this.objectArrayCodec = getCodec(codecMap, Object[].class);
            this.collectionCodec = getCodec(codecMap, Collection.class);
            this.mapCodec = getCodec(codecMap, Map.class);
        }

        private static <T> DsonCodecImpl<T> getCodec(Map<Class<?>, DsonCodecImpl<?>> codecMap, Class<T> clazz) {
            DsonCodecImpl<T> codec = (DsonCodecImpl<T>) codecMap.get(clazz);
            if (codec == null) throw new IllegalArgumentException(clazz.getName());
            return codec;
        }

        @Nullable
        @Override
        public <T> DsonCodecImpl<T> get(Class<T> clazz) {
            DsonCodecImpl<?> codec = codecMap.get(clazz);
            if (codec != null) return (DsonCodecImpl<T>) codec;

            if (clazz.isArray()) return (DsonCodecImpl<T>) objectArrayCodec;
            if (Collection.class.isAssignableFrom(clazz)) return (DsonCodecImpl<T>) collectionCodec;
            if (Map.class.isAssignableFrom(clazz)) return (DsonCodecImpl<T>) mapCodec;
            return null;
        }
    }

}