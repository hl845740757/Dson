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

import javax.annotation.Nullable;
import java.util.Collection;
import java.util.Map;

/**
 * @author wjybxx
 * date 2023/4/1
 */
public class DsonLiteConverterUtils extends ConverterUtils {

    /** 默认codec注册表 */
    private static final DsonLiteCodecRegistry CODEC_REGISTRY;

    static {
        Map<Class<?>, DsonLiteCodecImpl<?>> codecMap = DsonLiteCodecRegistries.newCodecMap(BUILTIN_CODECS.stream()
                .map(DsonLiteCodecImpl::new)
                .toList());
        CODEC_REGISTRY = new DefaultCodecRegistry(codecMap);
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