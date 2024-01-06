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

package cn.wjybxx.dson.codec;

import cn.wjybxx.dson.codec.dson.DsonCodec;
import cn.wjybxx.dson.codec.dson.DsonCodecScanIgnore;
import cn.wjybxx.dson.codec.dson.DsonObjectReader;
import cn.wjybxx.dson.codec.dson.DsonObjectWriter;
import cn.wjybxx.dson.codec.dsonlite.DsonLiteCodec;
import cn.wjybxx.dson.codec.dsonlite.DsonLiteCodecScanIgnore;
import cn.wjybxx.dson.codec.dsonlite.DsonLiteObjectReader;
import cn.wjybxx.dson.codec.dsonlite.DsonLiteObjectWriter;
import cn.wjybxx.dson.text.ObjectStyle;

import javax.annotation.Nonnull;

/**
 * 我们让生成的代码都实现该类，以减少生成的类数量
 * <p>
 * 1.继承的方法需要再次声明，以便注解处理器查找
 * 2.生成的代码不支持响应的序列化时，会自动添加对应的注解{@link DsonLiteCodecScanIgnore}{@link DsonCodecScanIgnore}
 *
 * @author wjybxx
 * date - 2023/12/10
 */
public interface DuplexCodec<T> extends DsonLiteCodec<T>, DsonCodec<T> {

    @Nonnull
    @Override
    Class<T> getEncoderClass();

    // Binary
    @Override
    void writeObject(DsonLiteObjectWriter writer, T instance, TypeArgInfo<?> typeArgInfo);

    @Override
    T readObject(DsonLiteObjectReader reader, TypeArgInfo<?> typeArgInfo);

    // Document
    @Override
    void writeObject(DsonObjectWriter writer, T instance, TypeArgInfo<?> typeArgInfo, ObjectStyle style);

    @Override
    T readObject(DsonObjectReader reader, TypeArgInfo<?> typeArgInfo);

    // common
    @Override
    default boolean isWriteAsArray() {
        return ConverterUtils.isEncodeAsArray(getEncoderClass());
    }

    @Override
    default boolean autoStartEnd() {
        return true;
    }

}