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

import cn.wjybxx.dson.codec.DsonCodecException;

import javax.annotation.Nullable;

/**
 * 编解码器注册表
 *
 * @author wjybxx
 * date 2023/4/3
 */
public interface DsonCodecRegistry {

    /**
     * 获取指定类class对应的编解码器
     */
    @Nullable
    <T> DsonCodecImpl<T> get(Class<T> clazz);

    default <T> DsonCodecImpl<T> getOrThrow(Class<T> clazz) {
        DsonCodecImpl<T> codec = get(clazz);
        if (codec == null) {
            throw new DsonCodecException("codec is absent, clazz" + clazz);
        }
        return codec;
    }

}