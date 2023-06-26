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

package cn.wjybxx.dson.codec.binary;

import cn.wjybxx.dson.DsonBinaryLiteReader;
import cn.wjybxx.dson.DsonBinaryLiteWriter;
import cn.wjybxx.dson.codec.*;
import cn.wjybxx.dson.codec.binary.codecs.MessageCodec;
import cn.wjybxx.dson.codec.binary.codecs.MessageEnumCodec;
import cn.wjybxx.dson.internal.CollectionUtils;
import cn.wjybxx.dson.io.*;
import com.google.protobuf.MessageLite;
import com.google.protobuf.ProtocolMessageEnum;

import javax.annotation.Nonnull;
import javax.annotation.Nullable;
import java.util.*;

/**
 * @author wjybxx
 * date 2023/4/2
 */
public class DefaultBinaryConverter implements BinaryConverter {

    final ClassIdRegistry<ClassId> classIdRegistry;
    final BinaryCodecRegistry codecRegistry;
    final ConvertOptions options;

    private DefaultBinaryConverter(ClassIdRegistry<ClassId> classIdRegistry,
                                   BinaryCodecRegistry codecRegistry,
                                   ConvertOptions options) {
        this.codecRegistry = codecRegistry;
        this.classIdRegistry = classIdRegistry;
        this.options = options;
    }

    @Override
    public BinaryCodecRegistry codecRegistry() {
        return codecRegistry;
    }

    @Override
    public ClassIdRegistry<ClassId> classIdRegistry() {
        return classIdRegistry;
    }

    @Override
    public void write(Object value, Chunk chunk, TypeArgInfo<?> typeArgInfo) {
        Objects.requireNonNull(value);
        final DsonOutput outputStream = DsonOutputs.newInstance(chunk.getBuffer(), chunk.getOffset(), chunk.getLength());
        encodeObject(outputStream, value, typeArgInfo);
        chunk.setUsed(outputStream.position());
    }

    @Override
    public <U> U read(Chunk chunk, TypeArgInfo<U> typeArgInfo) {
        final DsonInput inputStream = DsonInputs.newInstance(chunk.getBuffer(), chunk.getOffset(), chunk.getLength());
        return decodeObject(inputStream, typeArgInfo);
    }

    @Nonnull
    @Override
    public byte[] write(Object value, @Nonnull TypeArgInfo<?> typeArgInfo) {
        Objects.requireNonNull(value);
        final byte[] localBuffer = BufferPool.allocateBuffer();
        try {
            final Chunk chunk = new Chunk(localBuffer);
            write(value, chunk, typeArgInfo);

            final byte[] result = new byte[chunk.getUsed()];
            System.arraycopy(localBuffer, 0, result, 0, result.length);
            return result;
        } finally {
            BufferPool.releaseBuffer(localBuffer);
        }
    }

    @Override
    public <U> U cloneObject(Object value, TypeArgInfo<U> typeArgInfo) {
        Objects.requireNonNull(value);
        final byte[] localBuffer = BufferPool.allocateBuffer();
        try {
            final DsonOutput outputStream = DsonOutputs.newInstance(localBuffer);
            encodeObject(outputStream, value, typeArgInfo);

            final DsonInput inputStream = DsonInputs.newInstance(localBuffer, 0, outputStream.position());
            return decodeObject(inputStream, typeArgInfo);
        } finally {
            BufferPool.releaseBuffer(localBuffer);
        }
    }

    private void encodeObject(DsonOutput outputStream, @Nullable Object value, TypeArgInfo<?> typeArgInfo) {
        try (BinaryObjectWriter wrapper = new DefaultBinaryObjectWriter(this,
                new DsonBinaryLiteWriter(options.recursionLimit, outputStream))) {
            wrapper.writeObject(value, typeArgInfo);
            wrapper.flush();
        }
    }

    private <U> U decodeObject(DsonInput inputStream, TypeArgInfo<U> typeArgInfo) {
        try (BinaryObjectReader wrapper = new DefaultBinaryObjectReader(this,
                new DsonBinaryLiteReader(options.recursionLimit, inputStream))) {
            return wrapper.readObject(typeArgInfo);
        }
    }

    // ------------------------------------------------- 工厂方法 ------------------------------------------------------

    /**
     * @param allProtoBufClasses 所有的protobuf类
     * @param pojoCodecImplList  所有的普通对象编解码器，外部传入，因此用户可以处理冲突后传入
     * @param typeIdMap          所有的类型id信息，包括protobuf的类
     * @param options            一些可选项
     */
    @SuppressWarnings("unchecked")
    public static DefaultBinaryConverter newInstance(final Set<Class<?>> allProtoBufClasses,
                                                     final List<? extends BinaryPojoCodecImpl<?>> pojoCodecImplList,
                                                     final Map<Class<?>, ClassId> typeIdMap,
                                                     final ConvertOptions options) {
        Objects.requireNonNull(options, "options");
        // 检查classId是否存在，以及命名空间是否非法
        for (Class<?> clazz : allProtoBufClasses) {
            ClassId classId = CollectionUtils.getOrThrow(typeIdMap, clazz, "class");
            if (classId.isDefaultNameSpace()) {
                throw new IllegalArgumentException("bad classId " + classId + ", class " + clazz);
            }
        }
        for (BinaryPojoCodecImpl<?> codecImpl : pojoCodecImplList) {
            ClassId classId = CollectionUtils.getOrThrow(typeIdMap, codecImpl.getEncoderClass(), "class");
            if (classId.isDefaultNameSpace()) {
                throw new IllegalArgumentException("bad classId " + classId + ", class " + codecImpl.getEncoderClass());
            }
        }

        final List<BinaryPojoCodec<?>> allPojoCodecList = new ArrayList<>(allProtoBufClasses.size() + pojoCodecImplList.size());
        // 解析parser
        for (Class<?> messageClazz : allProtoBufClasses) {
            // protoBuf消息
            if (MessageLite.class.isAssignableFrom(messageClazz)) {
                MessageCodec<?> messageCodec = parseMessageCodec((Class<? extends MessageLite>) messageClazz);
                allPojoCodecList.add(new BinaryPojoCodec<>(messageCodec));
                continue;
            }
            // protoBuf枚举
            if (ProtocolMessageEnum.class.isAssignableFrom(messageClazz)) {
                MessageEnumCodec<?> enumCodec = parseMessageEnumCodec((Class<? extends ProtocolMessageEnum>) messageClazz);
                allPojoCodecList.add(new BinaryPojoCodec<>(enumCodec));
                continue;
            }
            throw new IllegalArgumentException("Unsupported class " + messageClazz);
        }
        // 转换codecImpl
        for (BinaryPojoCodecImpl<?> codecImpl : pojoCodecImplList) {
            allPojoCodecList.add(new BinaryPojoCodec<>(codecImpl));
        }

        final ClassIdRegistry<ClassId> classIdRegistry = ClassIdRegistries.fromRegistries(
                ClassIdRegistries.fromClassIdMap(typeIdMap),
                BinaryConverterUtils.getDefaultClassIdRegistry());

        final BinaryCodecRegistry codecRegistry = BinaryCodecRegistries.fromRegistries(
                BinaryCodecRegistries.fromPojoCodecs(allPojoCodecList),
                BinaryConverterUtils.getDefaultCodecRegistry());

        return new DefaultBinaryConverter(classIdRegistry, codecRegistry, options);
    }

    private static <T extends MessageLite> MessageCodec<T> parseMessageCodec(Class<T> messageClazz) {
        final var enumLiteMap = ProtobufUtils.findParser(messageClazz);
        return new MessageCodec<>(messageClazz, enumLiteMap);
    }

    private static <T extends ProtocolMessageEnum> MessageEnumCodec<T> parseMessageEnumCodec(Class<T> messageClazz) {
        final var enumLiteMap = ProtobufUtils.findMapper(messageClazz);
        return new MessageEnumCodec<>(messageClazz, enumLiteMap);
    }

}