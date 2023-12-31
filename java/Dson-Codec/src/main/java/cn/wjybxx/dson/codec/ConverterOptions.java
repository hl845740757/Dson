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

import cn.wjybxx.base.OptionalBool;
import cn.wjybxx.base.io.LocalByteArrayPool;
import cn.wjybxx.base.io.LocalStringBuilderPool;
import cn.wjybxx.base.pool.ObjectPool;
import cn.wjybxx.dson.DsonReaderSettings;
import cn.wjybxx.dson.DsonWriterSettings;
import cn.wjybxx.dson.codec.codecs.MapCodec;
import cn.wjybxx.dson.codec.dsonlite.DsonLiteConverter;
import cn.wjybxx.dson.text.DsonMode;
import cn.wjybxx.dson.text.DsonTextReaderSettings;
import cn.wjybxx.dson.text.DsonTextWriterSettings;

import javax.annotation.concurrent.Immutable;
import java.util.Objects;

/**
 * @author wjybxx
 * date - 2023/4/17
 */
@Immutable
public class ConverterOptions {

    /** classId的写入策略 */
    public final ClassIdPolicy classIdPolicy;
    /** protoBuf对应的二进制子类型 */
    public final int pbBinaryType;
    /**
     * 是否写入对象基础类型字段的默认值
     * 1.数值类型默认值为0，bool类型默认值为false
     * 2.只在Object上下文生效
     * <p>
     * 基础值类型需要单独控制，因为有时候我们仅想不输出null，但要输出基础类型字段的默认值 -- 通常是在文本模式下。
     * 强烈建议在{@link DsonLiteConverter}下不写入默认值和null
     */
    public final boolean appendDef;
    /**
     * 是否写入对象内的null值
     * 1.只在Object上下文生效
     * 2.对于一般的对象可不写入，因为ObjectReader是支持随机读的
     */
    public final boolean appendNull;
    /**
     * 是否把Map编码为普通对象（文档）
     * 1.只在文档编解码中生效
     * 2.如果要将一个Map结构编码为普通对象，<b>Key的运行时必须和声明类型相同</b>，且只支持String、Integer、Long、EnumLite。
     * 3.即使不开启该选项，用户也可以通过定义字段的writeProxy实现将Map写为普通Object - 可参考{@link MapCodec}
     *
     * <h3>Map不是Object</h3>
     * 本质上讲，Map是数组，而不是普通的Object，因为标准的Map是允许复杂key的，因此Map默认应该序列化为数组。但存在两个特殊的场景：
     * 1.与脚本语言通信
     * 脚本语言通常没有静态语言中的字典结构，由object充当，但object不支持复杂的key作为键，通常仅支持数字和字符串作为key。
     * 因此在与脚本语言通信时，要求将Map序列化为简单的object。
     * 2.配置文件读写
     * 配置文件通常是无类型的，因此读取到内存中通常是一个字典结构；程序在输出配置文件时，同样需要将字典结构输出为object结构。
     */
    public final boolean writeMapAsDocument;

    /** 数字classId的转换器 */
    public final ClassIdConverter classIdConverter;
    /** 字节数组缓存池 */
    public final ObjectPool<byte[]> bufferPool;
    /** 字符串缓存池 */
    public final ObjectPool<StringBuilder> stringBuilderPool;

    /** 二进制解码设置 */
    public final DsonReaderSettings binReaderSettings;
    /** 二进制编码设置 */
    public final DsonWriterSettings binWriterSettings;
    /** 文本解码设置 */
    public final DsonTextReaderSettings textReaderSettings;
    /** 文本编码设置 */
    public final DsonTextWriterSettings standardWriterSettings;
    /** 宽松文本writer设置 */
    public final DsonTextWriterSettings relaxedWriterSettings;

    public ConverterOptions(Builder builder) {
        this.classIdPolicy = builder.classIdPolicy;
        this.pbBinaryType = builder.pbBinaryType;
        this.appendDef = builder.appendDef.orElse(true);
        this.appendNull = builder.appendNull.orElse(true);
        this.writeMapAsDocument = builder.writeMapAsDocument.orElse(false);

        this.classIdConverter = builder.classIdConverter;
        this.bufferPool = builder.bufferPool;
        this.stringBuilderPool = builder.stringBuilderPool;

        this.binReaderSettings = Objects.requireNonNull(builder.binReaderSettings);
        this.binWriterSettings = Objects.requireNonNull(builder.binWriterSettings);
        this.textReaderSettings = Objects.requireNonNull(builder.textReaderSettings);
        this.standardWriterSettings = Objects.requireNonNull(builder.standardWriterSettings);
        this.relaxedWriterSettings = Objects.requireNonNull(builder.relaxedWriterSettings);

        if (standardWriterSettings.dsonMode != DsonMode.STANDARD) {
            throw new IllegalArgumentException("standardWriterSettings.dsonMode must be STANDARD");
        }
        if (relaxedWriterSettings.dsonMode != DsonMode.RELAXED) {
            throw new IllegalArgumentException("relaxedWriterSettings.dsonMode must be RELAXED");
        }
    }

    public static ConverterOptions DEFAULT = newBuilder().build();

    public static Builder newBuilder() {
        return new Builder();
    }

    public static class Builder {

        private ClassIdPolicy classIdPolicy = ClassIdPolicy.OPTIMIZED;
        private int pbBinaryType = 127;
        private OptionalBool appendDef = OptionalBool.TRUE;
        private OptionalBool appendNull = OptionalBool.TRUE;
        private OptionalBool writeMapAsDocument = OptionalBool.FALSE;

        private ClassIdConverter classIdConverter = new DefaultClassIdConverter();
        private ObjectPool<byte[]> bufferPool = LocalByteArrayPool.INSTANCE;
        private ObjectPool<StringBuilder> stringBuilderPool = LocalStringBuilderPool.INSTANCE;

        private DsonReaderSettings binReaderSettings = DsonReaderSettings.DEFAULT;
        private DsonWriterSettings binWriterSettings = DsonWriterSettings.DEFAULT;
        private DsonTextReaderSettings textReaderSettings = DsonTextReaderSettings.DEFAULT;
        private DsonTextWriterSettings standardWriterSettings = DsonTextWriterSettings.DEFAULT;
        private DsonTextWriterSettings relaxedWriterSettings = DsonTextWriterSettings.RELAXED_DEFAULT;

        public ClassIdPolicy getClassIdPolicy() {
            return classIdPolicy;
        }

        public Builder setClassIdPolicy(ClassIdPolicy classIdPolicy) {
            this.classIdPolicy = Objects.requireNonNull(classIdPolicy);
            return this;
        }

        public OptionalBool getAppendDef() {
            return appendDef;
        }

        public Builder setAppendDef(OptionalBool appendDef) {
            this.appendDef = appendDef;
            return this;
        }

        public OptionalBool getAppendNull() {
            return appendNull;
        }

        public Builder setAppendNull(OptionalBool appendNull) {
            this.appendNull = Objects.requireNonNull(appendNull);
            return this;
        }

        public OptionalBool getWriteMapAsDocument() {
            return writeMapAsDocument;
        }

        public Builder setWriteMapAsDocument(OptionalBool writeMapAsDocument) {
            this.writeMapAsDocument = Objects.requireNonNull(writeMapAsDocument);
            return this;
        }

        public int getPbBinaryType() {
            return pbBinaryType;
        }

        public Builder setPbBinaryType(int pbBinaryType) {
            this.pbBinaryType = pbBinaryType;
            return this;
        }

        public ClassIdConverter getClassIdResolver() {
            return classIdConverter;
        }

        public Builder setClassIdResolver(ClassIdConverter classIdConverter) {
            this.classIdConverter = Objects.requireNonNull(classIdConverter);
            return this;
        }

        public ObjectPool<byte[]> getBufferPool() {
            return bufferPool;
        }

        public Builder setBufferPool(ObjectPool<byte[]> bufferPool) {
            this.bufferPool = Objects.requireNonNull(bufferPool);
            return this;
        }

        public ObjectPool<StringBuilder> getStringBuilderPool() {
            return stringBuilderPool;
        }

        public Builder setStringBuilderPool(ObjectPool<StringBuilder> stringBuilderPool) {
            this.stringBuilderPool = stringBuilderPool;
            return this;
        }

        public ClassIdConverter getClassIdConverter() {
            return classIdConverter;
        }

        public Builder setClassIdConverter(ClassIdConverter classIdConverter) {
            this.classIdConverter = classIdConverter;
            return this;
        }

        public DsonReaderSettings getBinReaderSettings() {
            return binReaderSettings;
        }

        public Builder setBinReaderSettings(DsonReaderSettings binReaderSettings) {
            this.binReaderSettings = binReaderSettings;
            return this;
        }

        public DsonWriterSettings getBinWriterSettings() {
            return binWriterSettings;
        }

        public Builder setBinWriterSettings(DsonWriterSettings binWriterSettings) {
            this.binWriterSettings = binWriterSettings;
            return this;
        }

        public DsonTextReaderSettings getTextReaderSettings() {
            return textReaderSettings;
        }

        public Builder setTextReaderSettings(DsonTextReaderSettings textReaderSettings) {
            this.textReaderSettings = textReaderSettings;
            return this;
        }

        public DsonTextWriterSettings getStandardWriterSettings() {
            return standardWriterSettings;
        }

        public Builder setStandardWriterSettings(DsonTextWriterSettings standardWriterSettings) {
            this.standardWriterSettings = standardWriterSettings;
            return this;
        }

        public DsonTextWriterSettings getRelaxedWriterSettings() {
            return relaxedWriterSettings;
        }

        public Builder setRelaxedWriterSettings(DsonTextWriterSettings relaxedWriterSettings) {
            this.relaxedWriterSettings = relaxedWriterSettings;
            return this;
        }

        public ConverterOptions build() {
            return new ConverterOptions(this);
        }
    }

}