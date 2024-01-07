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

import javax.annotation.Nullable;
import java.util.Collection;
import java.util.Map;

/**
 * @author wjybxx
 * date 2023/4/4
 */
public class DsonConverterUtils extends ConverterUtils {

    public static final String NUMBER_KEY = "number";
    /** 默认codec注册表 */
    private static final DsonCodecRegistry CODEC_REGISTRY;

    static {
        Map<Class<?>, DsonCodecImpl<?>> codecMap = DsonCodecRegistries.newCodecMap(BUILTIN_CODECS.stream()
                .map(DsonCodecImpl::new)
                .toList());
        CODEC_REGISTRY = new DefaultCodecRegistry(codecMap);
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