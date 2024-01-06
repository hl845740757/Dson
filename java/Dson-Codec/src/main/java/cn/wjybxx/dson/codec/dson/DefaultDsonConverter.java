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

import cn.wjybxx.base.io.StringBuilderWriter;
import cn.wjybxx.dson.*;
import cn.wjybxx.dson.codec.ConverterOptions;
import cn.wjybxx.dson.codec.TypeArgInfo;
import cn.wjybxx.dson.codec.TypeMetaRegistries;
import cn.wjybxx.dson.codec.TypeMetaRegistry;
import cn.wjybxx.dson.io.*;
import cn.wjybxx.dson.text.*;

import javax.annotation.Nonnull;
import javax.annotation.Nullable;
import java.io.Reader;
import java.io.Writer;
import java.util.ArrayList;
import java.util.List;
import java.util.Objects;

/**
 * @author wjybxx
 * date 2023/4/2
 */
public class DefaultDsonConverter implements DsonConverter {

    final TypeMetaRegistry typeMetaRegistry;
    final DsonCodecRegistry codecRegistry;
    final ConverterOptions options;

    private DefaultDsonConverter(TypeMetaRegistry typeMetaRegistry,
                                 DsonCodecRegistry codecRegistry,
                                 ConverterOptions options) {
        this.codecRegistry = codecRegistry;
        this.typeMetaRegistry = typeMetaRegistry;
        this.options = options;
    }

    @Override
    public DsonCodecRegistry codecRegistry() {
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
        try (DsonObjectWriter wrapper = new DefaultDsonObjectWriter(this,
                new DsonBinaryWriter(options.binWriterSettings, outputStream))) {
            wrapper.writeObject(value, typeArgInfo, null);
            wrapper.flush();
        }
    }

    private <U> U decodeObject(DsonInput inputStream, TypeArgInfo<U> typeArgInfo) {
        try (DsonReader binaryReader = new DsonBinaryReader(options.binReaderSettings, inputStream)) {
            DsonObjectReader wrapper;
            if (!options.appendDef || !options.appendNull) { // 二进制数据不全
                wrapper = new DefaultDsonObjectReader(this, toDsonObjectReader(binaryReader));
            } else {
                wrapper = new SequentialObjectReader(this, binaryReader);
            }
            return wrapper.readObject(typeArgInfo);
        }
    }

    private DsonCollectionReader toDsonObjectReader(DsonReader dsonReader) {
        DsonValue dsonValue = Dsons.readTopDsonValue(dsonReader);
        return new DsonCollectionReader(options.binReaderSettings, new DsonArray<String>().append(dsonValue));
    }

    @Nonnull
    @Override
    public String writeAsDson(Object value, DsonMode dsonMode, @Nonnull TypeArgInfo<?> typeArgInfo) {
        StringBuilder stringBuilder = options.stringBuilderPool.rent();
        try {
            writeAsDson(value, dsonMode, typeArgInfo, new StringBuilderWriter(stringBuilder));
            return stringBuilder.toString();
        } finally {
            options.stringBuilderPool.returnOne(stringBuilder);
        }
    }

    @Override
    public <U> U readFromDson(CharSequence source, @Nonnull TypeArgInfo<U> typeArgInfo) {
        try (DsonReader textReader = new DsonTextReader(options.textReaderSettings, source);
             DsonObjectReader wrapper = new DefaultDsonObjectReader(this, toDsonObjectReader(textReader))) {
            return wrapper.readObject(typeArgInfo);
        }
    }

    @Override
    public void writeAsDson(Object value, DsonMode dsonMode, @Nonnull TypeArgInfo<?> typeArgInfo, Writer writer) {
        Objects.requireNonNull(writer, "writer");
        DsonTextWriterSettings writerSettings = dsonMode == DsonMode.RELAXED ? options.relaxedWriterSettings : options.standardWriterSettings;
        try (DsonObjectWriter wrapper = new DefaultDsonObjectWriter(this,
                new DsonTextWriter(writerSettings, writer))) {
            wrapper.writeObject(value, typeArgInfo, null);
            wrapper.flush();
        }
    }

    @Override
    public <U> U readFromDson(Reader source, @Nonnull TypeArgInfo<U> typeArgInfo) {
        try (DsonReader textReader = new DsonTextReader(options.textReaderSettings, Dsons.newStreamScanner(source));
             DsonObjectReader wrapper = new DefaultDsonObjectReader(this, toDsonObjectReader(textReader))) {
            return wrapper.readObject(typeArgInfo);
        }
    }

    @Override
    public DsonValue writeAsDsonValue(Object value, TypeArgInfo<?> typeArgInfo) {
        Objects.requireNonNull(value);
        DsonArray<String> outList = new DsonArray<>(1);
        try (DsonWriter objectWriter = new DsonCollectionWriter(options.binWriterSettings, outList);
             DsonObjectWriter wrapper = new DefaultDsonObjectWriter(this, objectWriter)) {
            wrapper.writeObject(value, typeArgInfo, ObjectStyle.INDENT);
            DsonValue dsonValue = outList.get(0);
            if (dsonValue.getDsonType().isContainer()) {
                return dsonValue;
            }
            throw new IllegalArgumentException("value must be container");
        }
    }

    @Override
    public <U> U readFromDsonValue(DsonValue source, @Nonnull TypeArgInfo<U> typeArgInfo) {
        if (!source.getDsonType().isContainer()) {
            throw new IllegalArgumentException("value must be container");
        }
        try (DsonReader objectReader = new DsonCollectionReader(options.binReaderSettings, new DsonArray<String>().append(source));
             DsonObjectReader wrapper = new DefaultDsonObjectReader(this, toDsonObjectReader(objectReader))) {
            return wrapper.readObject(typeArgInfo);
        }
    }

    // ------------------------------------------------- 工厂方法 ------------------------------------------------------

    /**
     * @param pojoCodecImplList 所有的普通对象编解码器，外部传入，因此用户可以处理冲突后传入
     * @param typeMetaRegistry  所有的类型id信息，包括protobuf的类
     * @param options           一些可选项
     */
    public static DefaultDsonConverter newInstance(final List<? extends DsonCodec<?>> pojoCodecImplList,
                                                   final TypeMetaRegistry typeMetaRegistry,
                                                   final ConverterOptions options) {
        Objects.requireNonNull(options, "options");
        // 检查classId是否存在，以及命名是否非法
        for (DsonCodec<?> codecImpl : pojoCodecImplList) {
            typeMetaRegistry.checkedOfType(codecImpl.getEncoderClass());
        }

        // 转换codecImpl
        final List<DsonCodecImpl<?>> allPojoCodecList = new ArrayList<>(pojoCodecImplList.size());
        for (DsonCodec<?> codecImpl : pojoCodecImplList) {
            allPojoCodecList.add(new DsonCodecImpl<>(codecImpl));
        }
        return new DefaultDsonConverter(
                TypeMetaRegistries.fromRegistries(
                        typeMetaRegistry,
                        DsonConverterUtils.getDefaultTypeMetaRegistry()),
                DsonCodecRegistries.fromRegistries(
                        DsonCodecRegistries.fromPojoCodecs(allPojoCodecList),
                        DsonConverterUtils.getDefaultCodecRegistry()),
                options);
    }

    /**
     * @param registryList     可以包含一些特殊的registry
     * @param typeMetaRegistry 所有的类型id信息，包括protobuf的类
     * @param options          一些可选项
     * @return
     */
    public static DefaultDsonConverter newInstance2(final List<DsonCodecRegistry> registryList,
                                                    final TypeMetaRegistry typeMetaRegistry,
                                                    final ConverterOptions options) {

        ArrayList<DsonCodecRegistry> copied = new ArrayList<>(registryList);
        copied.add(DsonConverterUtils.getDefaultCodecRegistry());

        return new DefaultDsonConverter(
                TypeMetaRegistries.fromRegistries(
                        typeMetaRegistry,
                        DsonConverterUtils.getDefaultTypeMetaRegistry()),
                DsonCodecRegistries.fromRegistries(
                        copied.toArray(DsonCodecRegistry[]::new)),
                options);
    }
}