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

import javax.annotation.Nullable;
import java.util.IdentityHashMap;
import java.util.List;
import java.util.Map;

/**
 * @author wjybxx
 * date 2023/4/4
 */
public class DsonCodecRegistries {

    public static Map<Class<?>, DsonCodecImpl<?>> newCodecMap(List<? extends DsonCodecImpl<?>> pojoCodecs) {
        final IdentityHashMap<Class<?>, DsonCodecImpl<?>> codecMap = new IdentityHashMap<>(pojoCodecs.size());
        for (DsonCodecImpl<?> codec : pojoCodecs) {
            if (codecMap.containsKey(codec.getEncoderClass())) {
                throw new IllegalArgumentException("the class has multiple codecs :" + codec.getEncoderClass().getName());
            }
            codecMap.put(codec.getEncoderClass(), codec);
        }
        return codecMap;
    }

    public static DsonCodecRegistry fromPojoCodecs(List<? extends DsonCodecImpl<?>> pojoCodecs) {
        final IdentityHashMap<Class<?>, DsonCodecImpl<?>> identityHashMap = new IdentityHashMap<>(pojoCodecs.size());
        for (DsonCodecImpl<?> codec : pojoCodecs) {
            if (identityHashMap.put(codec.getEncoderClass(), codec) != null) {
                throw new IllegalArgumentException("the class has multiple codecs :" + codec.getEncoderClass().getName());
            }
        }
        return new DefaultCodecRegistry(identityHashMap);
    }

    public static DsonCodecRegistry fromRegistries(DsonCodecRegistry... codecRegistry) {
        return new CompositeCodecRegistry(List.of(codecRegistry));
    }

    private static class DefaultCodecRegistry implements DsonCodecRegistry {

        private final IdentityHashMap<Class<?>, DsonCodecImpl<?>> type2CodecMap;

        private DefaultCodecRegistry(IdentityHashMap<Class<?>, DsonCodecImpl<?>> type2CodecMap) {
            this.type2CodecMap = type2CodecMap;
        }

        @SuppressWarnings("unchecked")
        @Override
        public <T> DsonCodecImpl<T> get(Class<T> clazz) {
            return (DsonCodecImpl<T>) type2CodecMap.get(clazz);
        }

    }

    private static class CompositeCodecRegistry implements DsonCodecRegistry {

        private final List<DsonCodecRegistry> registryList;

        private CompositeCodecRegistry(List<DsonCodecRegistry> registryList) {
            this.registryList = registryList;
        }

        @Nullable
        @Override
        public <T> DsonCodecImpl<T> get(Class<T> clazz) {
            List<DsonCodecRegistry> registryList = this.registryList;
            for (int i = 0; i < registryList.size(); i++) {
                DsonCodecRegistry registry = registryList.get(i);
                DsonCodecImpl<T> codec = registry.get(clazz);
                if (codec != null) return codec;
            }
            return null;
        }
    }

}