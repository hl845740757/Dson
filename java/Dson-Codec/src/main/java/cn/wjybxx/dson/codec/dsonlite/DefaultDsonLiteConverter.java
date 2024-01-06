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

import cn.wjybxx.dson.DsonLiteBinaryReader;
import cn.wjybxx.dson.DsonLiteBinaryWriter;
import cn.wjybxx.dson.codec.ConverterOptions;
import cn.wjybxx.dson.codec.TypeArgInfo;
import cn.wjybxx.dson.codec.TypeMetaRegistries;
import cn.wjybxx.dson.codec.TypeMetaRegistry;
import cn.wjybxx.dson.io.*;

import javax.annotation.Nonnull;
import javax.annotation.Nullable;
import java.util.ArrayList;
import java.util.List;
import java.util.Objects;

/**
 * @author wjybxx
 * date 2023/4/2
 */
public class DefaultDsonLiteConverter implements DsonLiteConverter {

    final TypeMetaRegistry typeMetaRegistry;
    final DsonLiteCodecRegistry codecRegistry;
    final ConverterOptions options;

    private DefaultDsonLiteConverter(TypeMetaRegistry typeMetaRegistry,
                                     DsonLiteCodecRegistry codecRegistry,
                                     ConverterOptions options) {
        this.codecRegistry = codecRegistry;
        this.typeMetaRegistry = typeMetaRegistry;
        this.options = options;
    }

    @Override
    public DsonLiteCodecRegistry codecRegistry() {
        return codecRegistry;
    }

    @Override
    public TypeMetaRegistry typeMetaRegistry() {
        return typeMetaRegistry;
    }

    @Override
    public ConverterOptions options() {
        return options;
    }

    @Override
    public void write(Object value, DsonChunk chunk, TypeArgInfo<?> typeArgInfo) {
        Objects.requireNonNull(value);
        final DsonOutput outputStream = DsonOutputs.newInstance(chunk.getBuffer(), chunk.getOffset(), chunk.getLength());
        encodeObject(outputStream, value, typeArgInfo);
        chunk.setUsed(outputStream.getPosition());
    }

    @Override
    public <U> U read(DsonChunk chunk, TypeArgInfo<U> typeArgInfo) {
        final DsonInput inputStream = DsonInputs.newInstance(chunk.getBuffer(), chunk.getOffset(), chunk.getLength());
        return decodeObject(inputStream, typeArgInfo);
    }

    @Nonnull
    @Override
    public byte[] write(Object value, @Nonnull TypeArgInfo<?> typeArgInfo) {
        Objects.requireNonNull(value);
        final byte[] localBuffer = options.bufferPool.rent();
        try {
            final DsonChunk chunk = new DsonChunk(localBuffer);
            write(value, chunk, typeArgInfo);

            final byte[] result = new byte[chunk.getUsed()];
            System.arraycopy(localBuffer, 0, result, 0, result.length);
            return result;
        } finally {
            options.bufferPool.returnOne(localBuffer);
        }
    }

    @Override
    public <U> U cloneObject(Object value, TypeArgInfo<U> typeArgInfo) {
        Objects.requireNonNull(value);
        final byte[] localBuffer = options.bufferPool.rent();
        try {
            final DsonOutput outputStream = DsonOutputs.newInstance(localBuffer);
            encodeObject(outputStream, value, typeArgInfo);

            final DsonInput inputStream = DsonInputs.newInstance(localBuffer, 0, outputStream.getPosition());
            return decodeObject(inputStream, typeArgInfo);
        } finally {
            options.bufferPool.returnOne(localBuffer);
        }
    }

    private void encodeObject(DsonOutput outputStream, @Nullable Object value, TypeArgInfo<?> typeArgInfo) {
        try (DsonLiteObjectWriter wrapper = new DefaultDsonLiteObjectWriter(this,
                new DsonLiteBinaryWriter(options.binWriterSettings, outputStream))) {
            wrapper.writeObject(value, typeArgInfo);
            wrapper.flush();
        }
    }

    private <U> U decodeObject(DsonInput inputStream, TypeArgInfo<U> typeArgInfo) {
        try (DsonLiteObjectReader wrapper = new DefaultDsonLiteObjectReader(this,
                new DsonLiteBinaryReader(options.binReaderSettings, inputStream))) {
            return wrapper.readObject(typeArgInfo);
        }
    }

    // ------------------------------------------------- 工厂方法 ------------------------------------------------------

    /**
     * @param pojoCodecImplList 所有的普通对象编解码器，外部传入，因此用户可以处理冲突后传入
     * @param typeMetaRegistry  所有的类型id信息，包括protobuf的类
     * @param options           一些可选项
     */
    public static DefaultDsonLiteConverter newInstance(final List<? extends DsonLiteCodec<?>> pojoCodecImplList,
                                                       final TypeMetaRegistry typeMetaRegistry,
                                                       final ConverterOptions options) {
        Objects.requireNonNull(options, "options");
        // 检查classId是否存在，以及命名空间是否非法
        for (DsonLiteCodec<?> codecImpl : pojoCodecImplList) {
            typeMetaRegistry.checkedOfType(codecImpl.getEncoderClass());
        }

        // 转换codecImpl
        List<DsonLiteCodecImpl<?>> allPojoCodecList = new ArrayList<>(pojoCodecImplList.size());
        for (DsonLiteCodec<?> codecImpl : pojoCodecImplList) {
            allPojoCodecList.add(new DsonLiteCodecImpl<>(codecImpl));
        }

        return new DefaultDsonLiteConverter(
                TypeMetaRegistries.fromRegistries(
                        typeMetaRegistry,
                        DsonLiteConverterUtils.getDefaultTypeMetaRegistry()),
                DsonLiteCodecRegistries.fromRegistries(
                        DsonLiteCodecRegistries.fromPojoCodecs(allPojoCodecList),
                        DsonLiteConverterUtils.getDefaultCodecRegistry()),
                options);
    }

    /**
     * @param registryList     可以包含一些特殊的可以包含一些特殊的registry
     * @param typeMetaRegistry 所有的类型id信息，包括protobuf的类
     * @param options          一些可选项
     */
    public static DefaultDsonLiteConverter newInstance2(final List<DsonLiteCodecRegistry> registryList,
                                                        final TypeMetaRegistry typeMetaRegistry,
                                                        final ConverterOptions options) {
        ArrayList<DsonLiteCodecRegistry> copied = new ArrayList<>(registryList);
        copied.add(DsonLiteConverterUtils.getDefaultCodecRegistry());

        return new DefaultDsonLiteConverter(
                TypeMetaRegistries.fromRegistries(
                        typeMetaRegistry,
                        DsonLiteConverterUtils.getDefaultTypeMetaRegistry()),
                DsonLiteCodecRegistries.fromRegistries(
                        copied.toArray(DsonLiteCodecRegistry[]::new)),
                options);

    }
}