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

import javax.annotation.Nullable;
import java.util.IdentityHashMap;
import java.util.List;
import java.util.Map;

/**
 * @author wjybxx
 * date 2023/3/31
 */
public class DsonLiteCodecRegistries {

    // region

    public static Map<Class<?>, DsonLiteCodecImpl<?>> newCodecMap(List<? extends DsonLiteCodecImpl<?>> pojoCodecs) {
        final IdentityHashMap<Class<?>, DsonLiteCodecImpl<?>> codecMap = new IdentityHashMap<>(pojoCodecs.size());
        for (DsonLiteCodecImpl<?> codec : pojoCodecs) {
            if (codecMap.put(codec.getEncoderClass(), codec) != null) {
                throw new IllegalArgumentException("the class has multiple codecs :" + codec.getEncoderClass().getName());
            }
            codecMap.put(codec.getEncoderClass(), codec);
        }
        return codecMap;
    }

    public static DsonLiteCodecRegistry fromPojoCodecs(List<? extends DsonLiteCodecImpl<?>> pojoCodecs) {
        final IdentityHashMap<Class<?>, DsonLiteCodecImpl<?>> identityHashMap = new IdentityHashMap<>(pojoCodecs.size());
        for (DsonLiteCodecImpl<?> codec : pojoCodecs) {
            if (identityHashMap.put(codec.getEncoderClass(), codec) != null) {
                throw new IllegalArgumentException("the class has multiple codecs :" + codec.getEncoderClass().getName());
            }
        }
        return new DefaultCodecRegistry(identityHashMap);
    }

    public static DsonLiteCodecRegistry fromRegistries(DsonLiteCodecRegistry... codecRegistry) {
        return new CompositeCodecRegistry(List.of(codecRegistry));
    }

    private static class DefaultCodecRegistry implements DsonLiteCodecRegistry {

        private final IdentityHashMap<Class<?>, DsonLiteCodecImpl<?>> type2CodecMap;

        private DefaultCodecRegistry(IdentityHashMap<Class<?>, DsonLiteCodecImpl<?>> type2CodecMap) {
            this.type2CodecMap = type2CodecMap;
        }

        @SuppressWarnings("unchecked")
        @Override
        public <T> DsonLiteCodecImpl<T> get(Class<T> clazz) {
            return (DsonLiteCodecImpl<T>) type2CodecMap.get(clazz);
        }

    }

    private static class CompositeCodecRegistry implements DsonLiteCodecRegistry {

        private final List<DsonLiteCodecRegistry> registryList;

        private CompositeCodecRegistry(List<DsonLiteCodecRegistry> registryList) {
            this.registryList = registryList;
        }

        @Nullable
        @Override
        public <T> DsonLiteCodecImpl<T> get(Class<T> clazz) {
            List<DsonLiteCodecRegistry> registryList = this.registryList;
            for (int i = 0; i < registryList.size(); i++) {
                DsonLiteCodecImpl<T> codec = registryList.get(i).get(clazz);
                if (codec != null) return codec;
            }
            return null;
        }
    }
    // endregion
}