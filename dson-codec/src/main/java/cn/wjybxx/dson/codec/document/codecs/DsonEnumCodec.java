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

package cn.wjybxx.dson.codec.document.codecs;

import cn.wjybxx.dson.codec.DsonEnum;
import cn.wjybxx.dson.codec.TypeArgInfo;
import cn.wjybxx.dson.codec.binary.BinaryPojoCodecImpl;
import cn.wjybxx.dson.codec.document.DocumentObjectReader;
import cn.wjybxx.dson.codec.document.DocumentObjectWriter;
import cn.wjybxx.dson.codec.document.DocumentPojoCodecImpl;

import javax.annotation.Nonnull;
import java.util.Objects;
import java.util.function.IntFunction;

/**
 * 在之前的版本中，生成的代码直接继承{@link BinaryPojoCodecImpl}，这让枚举的编解码调整较为麻烦，
 * 因为要调整APT，而让枚举的Codec继承该Codec的话，生成的代码就更为稳定，我们调整编解码方式也更方便。
 *
 * @author wjybxx
 * date - 2023/4/24
 */
public class DsonEnumCodec<T extends DsonEnum> implements DocumentPojoCodecImpl<T> {

    private final Class<T> encoderClass;
    private final IntFunction<T> mapper;
    private final String typeName;

    /**
     * @param forNumber forNumber静态方法的lambda表达式
     */
    public DsonEnumCodec(Class<T> encoderClass, IntFunction<T> forNumber, String typeName) {
        this.encoderClass = Objects.requireNonNull(encoderClass);
        this.mapper = Objects.requireNonNull(forNumber);
        this.typeName = Objects.requireNonNull(typeName);
    }

    public T forNumber(int number) {
        return mapper.apply(number);
    }

    @Nonnull
    @Override
    public String getTypeName() {
        return typeName;
    }

    @Nonnull
    @Override
    public Class<T> getEncoderClass() {
        return encoderClass;
    }

    @Override
    public T readObject(DocumentObjectReader reader, TypeArgInfo<?> typeArgInfo) {
        return mapper.apply(reader.readInt("number"));
    }

    @Override
    public void writeObject(T instance, DocumentObjectWriter writer, TypeArgInfo<?> typeArgInfo) {
        writer.writeInt("number", instance.getNumber());
    }

}