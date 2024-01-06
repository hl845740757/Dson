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

import cn.wjybxx.dson.codec.TypeArgInfo;
import cn.wjybxx.dson.codec.codecs.EnumLiteCodec;
import cn.wjybxx.dson.text.ObjectStyle;

import javax.annotation.Nonnull;
import java.util.Objects;

/**
 * @author wjybxx
 * date 2023/4/3
 */
public class DsonCodecImpl<T> {

    private final DsonCodec<T> codec;
    private final boolean isArray;

    public DsonCodecImpl(DsonCodec<T> codec) {
        Objects.requireNonNull(codec.getEncoderClass());
        this.codec = codec;
        this.isArray = codec.isWriteAsArray();
    }

    @Nonnull
    public Class<T> getEncoderClass() {
        return codec.getEncoderClass();
    }

    /** 是否编码为数组 */
    public boolean isWriteAsArray() {
        return isArray;
    }

    /**
     * 从输入流中解析指定对象。
     * 它应该创建对象，并反序列化该类及其所有超类定义的所有要序列化的字段。
     */
    public T readObject(DsonObjectReader reader, TypeArgInfo<?> typeArgInfo) {
        if (codec.autoStartEnd()) {
            T result;
            if (isArray) {
                reader.readStartArray(typeArgInfo);
                result = codec.readObject(reader, typeArgInfo);
                reader.readEndArray();
            } else {
                reader.readStartObject(typeArgInfo);
                result = codec.readObject(reader, typeArgInfo);
                reader.readEndObject();
            }
            return result;
        } else {
            return codec.readObject(reader, typeArgInfo);
        }
    }

    /**
     * 将对象写入输出流。
     * 将对象及其所有超类定义的所有要序列化的字段写入输出流。
     */
    public void writeObject(DsonObjectWriter writer, T instance, TypeArgInfo<?> typeArgInfo, ObjectStyle style) {
        if (codec.autoStartEnd()) {
            if (isArray) {
                writer.writeStartArray(instance, typeArgInfo, style);
                codec.writeObject(writer, instance, typeArgInfo, style);
                writer.writeEndArray();
            } else {
                writer.writeStartObject(instance, typeArgInfo, style);
                codec.writeObject(writer, instance, typeArgInfo, style);
                writer.writeEndObject();
            }
        } else {
            codec.writeObject(writer, instance, typeArgInfo, style);
        }
    }

    public boolean isEnumLiteCodec() {
        return codec instanceof EnumLiteCodec;
    }

    @SuppressWarnings("unchecked")
    public T forNumber(int number) {
        if (codec instanceof EnumLiteCodec<?> enumLiteCodec) {
            return (T) enumLiteCodec.forNumber(number);
        }
        throw new UnsupportedOperationException("unexpected forNumber method call");
    }

}