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

package cn.wjybxx.dson.io;

import com.google.protobuf.Parser;

import javax.annotation.Nonnull;

/**
 * 通过{@link DsonInputs}的静态方法创建实例
 *
 * @author wjybxx
 * date 2023/4/1
 */
public interface DsonInput extends AutoCloseable {

    byte readRawByte();

    //
    int readInt32();

    int readUint32();

    int readSint32();

    int readFixed32();

    //
    long readInt64();

    long readUint64();

    long readSint64();

    long readFixed64();

    //

    /** 该接口默认读取4字节 */
    float readFloat();

    /** 该接口默认读取8字节 */
    double readDouble();

    boolean readBool();

    String readString();

    /** @param size 要读取的字节数 */
    byte[] readRawBytes(int size);

    /** @param n 要跳过的字节数 */
    void skipRawBytes(int n);

    /**
     * 读取一个protoBuf消息，内容部分没有size
     */
    <T> T readMessage(@Nonnull Parser<T> parser);

    /** 当前读索引位置 - 已读字节数 */
    int getPosition();

    /**
     * 设置读索引位置
     *
     * @throws IllegalArgumentException 如果设置到目标位置
     */
    void setPosition(int readerIndex);

    default byte getByte(int readerIndex) {
        int oldPosition = getPosition();
        setPosition(readerIndex);
        byte value = readRawByte();
        setPosition(oldPosition);
        return value;
    }

    /**
     * 从指定位置读取一个fix32类型整数
     * 相比先{@link #setPosition(int)}再{@link #readFixed32()}的方式，该接口更容易优化实现
     */
    default int getFixed32(int readerIndex) {
        int oldPosition = getPosition();
        setPosition(readerIndex);
        int value = readFixed32();
        setPosition(oldPosition);
        return value;
    }

    /**
     * 限制接下来可读取的字节数
     *
     * @param byteLimit 可用字节数
     * @return oldLimit 前一次设置的限制点
     */
    int pushLimit(int byteLimit);

    /**
     * 恢复字节数限制
     *
     * @param oldLimit 前一次设置的限制点
     */
    void popLimit(int oldLimit);

    /** @return 剩余可用的字节数 */
    int getBytesUntilLimit();

    /** @return 是否达到输入流的末端 */
    boolean isAtEnd();

    @Override
    void close();
}