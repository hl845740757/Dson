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

package cn.wjybxx.dson;

import cn.wjybxx.dson.io.DsonChunk;
import cn.wjybxx.dson.text.INumberStyle;
import cn.wjybxx.dson.text.ObjectStyle;
import cn.wjybxx.dson.text.StringStyle;
import cn.wjybxx.dson.types.ObjectRef;
import cn.wjybxx.dson.types.OffsetTimestamp;
import com.google.protobuf.MessageLite;

import java.util.Objects;

/**
 * 1.写数组普通元素的时候，{@code name}传null，写嵌套对象时使用无name参数的start方法（实在不想定义太多的方法）
 * 2.double、boolean、null由于可以从无符号字符串精确解析得出，因此可以总是不输出类型标签，
 * 3.内置结构体总是输出类型标签，且总是Flow模式，可以降低使用复杂度；
 *
 * @author wjybxx
 * date - 2023/4/20
 */
@SuppressWarnings("unused")
public interface DsonWriter extends AutoCloseable {

    void flush();

    @Override
    void close();

    /**
     * 获取当前上下文的类型
     */
    DsonContextType getContextType();

    /** 当前是否处于等待写入name的状态 */
    boolean isAtName();

    /**
     * 编码的时候，用户总是习惯 name和value 同时写入，
     * 但在写Array或Object容器的时候，不能同时完成，需要先写入name再开始写值
     */
    void writeName(String name);

    // region 简单值

    void writeInt32(String name, int value, WireType wireType, INumberStyle style);

    void writeInt64(String name, long value, WireType wireType, INumberStyle style);

    void writeFloat(String name, float value, INumberStyle style);

    void writeDouble(String name, double value, INumberStyle style);

    void writeBoolean(String name, boolean value);

    void writeString(String name, String value, StringStyle style);

    void writeNull(String name);

    void writeBinary(String name, DsonBinary dsonBinary);

    void writeBinary(String name, int type, DsonChunk chunk);

    void writeExtInt32(String name, DsonExtInt32 extInt32, WireType wireType, INumberStyle style);

    void writeExtInt64(String name, DsonExtInt64 extInt64, WireType wireType, INumberStyle style);

    void writeExtDouble(String name, DsonExtDouble extDouble, INumberStyle style);

    void writeExtString(String name, DsonExtString extString, StringStyle style);

    void writeRef(String name, ObjectRef objectRef);

    void writeTimestamp(String name, OffsetTimestamp timestamp);

    // endregion

    // region 容器

    void writeStartArray(ObjectStyle style);

    void writeEndArray();

    void writeStartObject(ObjectStyle style);

    void writeEndObject();

    /** Header应该保持简单，因此通常应该使用Flow模式 */
    void writeStartHeader(ObjectStyle style);

    void writeEndHeader();

    /**
     * 开始写一个数组
     * 1.数组内元素没有名字，因此name传 null 即可
     *
     * <pre>{@code
     *      writer.writeStartArray(name, ObjectStyle.INDENT);
     *      for (String coderName: coderNames) {
     *          writer.writeString(null, coderName);
     *      }
     *      writer.writeEndArray();
     * }</pre>
     */
    default void writeStartArray(String name, ObjectStyle style) {
        writeName(name);
        writeStartArray(style);
    }

    /**
     * 开始写一个普通对象
     * <pre>{@code
     *      writer.writeStartObject(name, ObjectStyle.INDENT);
     *      writer.writeString("name", "wjybxx")
     *      writer.writeInt32("age", 28)
     *      writer.writeEndObject();
     * }</pre>
     */
    default void writeStartObject(String name, ObjectStyle style) {
        writeName(name);
        writeStartObject(style);
    }
    // endregion

    // region 特殊支持

    /** 仅有一个clsName属性的header */
    default void writeSimpleHeader(String clsName) {
        Objects.requireNonNull(clsName, "clsName");
        writeStartHeader(ObjectStyle.FLOW);
        writeString(DsonHeader.NAMES_CLASS_NAME, clsName, StringStyle.AUTO_QUOTE);
        writeEndHeader();
    }

    /**
     * Message最终会写为Binary
     */
    void writeMessage(String name, int binaryType, MessageLite messageLite);

    /**
     * 直接写入一个已编码的字节数组
     * 1.请确保合法性
     * 2.支持的类型与读方法相同
     *
     * @param data {@link DsonReader#readValueAsBytes(String)}读取的数据
     */
    void writeValueBytes(String name, DsonType type, byte[] data);

    /**
     * 附近一个数据到当前上下文
     *
     * @return 旧值
     */
    Object attach(Object userData);

    Object attachment();

    // endregion

}