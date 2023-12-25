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

package cn.wjybxx.dson.codec.document;

import cn.wjybxx.dson.codec.ConverterUtils;
import cn.wjybxx.dson.codec.TypeMeta;
import cn.wjybxx.dson.codec.TypeMetaRegistries;
import cn.wjybxx.dson.codec.TypeMetaRegistry;
import cn.wjybxx.dson.codec.codecs.*;
import cn.wjybxx.dson.codec.codecs.*;
import cn.wjybxx.dson.text.ObjectStyle;

import javax.annotation.Nullable;
import java.util.*;
import java.util.concurrent.ConcurrentHashMap;

/**
 * @author wjybxx
 * date 2023/4/4
 */
public class DocumentConverterUtils extends ConverterUtils {

    /** 类型id注册表 */
    private static final TypeMetaRegistry TYPE_META_REGISTRY;
    /** 默认codec注册表 */
    private static final DocumentCodecRegistry CODEC_REGISTRY;

    static {
        List<DocumentPojoCodec<?>> entryList = List.of(
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
                newCodec(new MapCodec<>(ConcurrentHashMap.class, ConcurrentHashMap::new))
        );

        Map<Class<?>, DocumentPojoCodec<?>> codecMap = DocumentCodecRegistries.newCodecMap(entryList);
        CODEC_REGISTRY = new DefaultCodecRegistry(codecMap);

        TYPE_META_REGISTRY = TypeMetaRegistries.fromMetas(
                entryOfClass(int[].class),
                entryOfClass(long[].class),
                entryOfClass(float[].class),
                entryOfClass(double[].class),
                entryOfClass(boolean[].class),
                entryOfClass(String[].class),
                entryOfClass(short[].class),
                entryOfClass(char[].class),

                entryOfClass(Object[].class),
                entryOfClass(Collection.class),
                entryOfClass(Map.class),

                // 常用具体类型集合
                entryOfClass(LinkedList.class),
                entryOfClass(ArrayDeque.class),
                entryOfClass(IdentityHashMap.class),
                entryOfClass(ConcurrentHashMap.class)
        );
    }

    private static TypeMeta entryOfClass(Class<?> clazz) {
        return TypeMeta.of(clazz, ObjectStyle.INDENT, clazz.getSimpleName());
    }

    private static <T> DocumentPojoCodec<T> newCodec(DocumentPojoCodecImpl<T> codecImpl) {
        return new DocumentPojoCodec<>(codecImpl);
    }

    public static DocumentCodecRegistry getDefaultCodecRegistry() {
        return CODEC_REGISTRY;
    }

    public static TypeMetaRegistry getDefaultTypeMetaRegistry() {
        return TYPE_META_REGISTRY;
    }

    @SuppressWarnings({"rawtypes", "unchecked"})
    private static class DefaultCodecRegistry implements DocumentCodecRegistry {

        final Map<Class<?>, DocumentPojoCodec<?>> codecMap;

        final DocumentPojoCodec<Object[]> objectArrayCodec;
        final DocumentPojoCodec<Collection> collectionCodec;
        final DocumentPojoCodec<Map> mapCodec;

        private DefaultCodecRegistry(Map<Class<?>, DocumentPojoCodec<?>> codecMap) {
            this.codecMap = codecMap;

            this.objectArrayCodec = getCodec(codecMap, Object[].class);
            this.collectionCodec = getCodec(codecMap, Collection.class);
            this.mapCodec = getCodec(codecMap, Map.class);
        }

        private static <T> DocumentPojoCodec<T> getCodec(Map<Class<?>, DocumentPojoCodec<?>> codecMap, Class<T> clazz) {
            DocumentPojoCodec<T> codec = (DocumentPojoCodec<T>) codecMap.get(clazz);
            if (codec == null) throw new IllegalArgumentException(clazz.getName());
            return codec;
        }

        @Nullable
        @Override
        public <T> DocumentPojoCodec<T> get(Class<T> clazz) {
            DocumentPojoCodec<?> codec = codecMap.get(clazz);
            if (codec != null) return (DocumentPojoCodec<T>) codec;

            if (clazz.isArray()) return (DocumentPojoCodec<T>) objectArrayCodec;
            if (Collection.class.isAssignableFrom(clazz)) return (DocumentPojoCodec<T>) collectionCodec;
            if (Map.class.isAssignableFrom(clazz)) return (DocumentPojoCodec<T>) mapCodec;
            return null;
        }
    }

}