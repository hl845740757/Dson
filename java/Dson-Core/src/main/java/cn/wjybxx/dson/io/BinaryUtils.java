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

import org.apache.commons.lang3.mutable.MutableInt;

import java.nio.Buffer;

/**
 * @author wjybxx
 * date 2023/3/31
 */
public class BinaryUtils {

    private BinaryUtils() {
    }

    public static void checkBuffer(byte[] buffer, int offset, int length) {
        checkBuffer(buffer.length, offset, length);
    }

    /**
     * 允许{@code offset + length == bufferLength}
     *
     * @param bufferLength buffer数组的长度
     * @param offset       数据的起始索引
     * @param length       数据的长度
     */
    public static void checkBuffer(int bufferLength, int offset, int length) {
        if ((offset | length | (bufferLength - (offset + length))) < 0) {
            throw new IllegalArgumentException(String.format("Array range is invalid. Buffer.length=%d, offset=%d, length=%d",
                    bufferLength, offset, length));
        }
    }

    public static void checkBuffer(int bufferLength, int offset) {
        if (offset < 0 || offset > bufferLength) {
            throw new IllegalArgumentException(String.format("Array range is invalid. Buffer.length=%d, offset=%d",
                    bufferLength, offset));
        }
    }

    /** JDK9+的代码跑在JDK8上的兼容问题 */
    public static void position(Buffer byteBuffer, int newOffset) {
        byteBuffer.position(newOffset);
    }

    // region 大端编码

    public static byte getByte(byte[] buffer, int index) {
        return buffer[index];
    }

    public static void setByte(byte[] buffer, int index, int value) {
        buffer[index] = (byte) value;
    }

    public static void setShort(byte[] buffer, int index, int value) {
        buffer[index] = (byte) (value >>> 8);
        buffer[index + 1] = (byte) value;
    }

    public static short getShort(byte[] buffer, int index) {
        return (short) ((buffer[index] << 8)
                | (buffer[index + 1] & 0xff));
    }

    public static void setInt(byte[] buffer, int index, int value) {
        buffer[index] = (byte) (value >>> 24);
        buffer[index + 1] = (byte) (value >>> 16);
        buffer[index + 2] = (byte) (value >>> 8);
        buffer[index + 3] = (byte) value;
    }

    public static int getInt(byte[] buffer, int index) {
        return (((buffer[index] & 0xff) << 24)
                | ((buffer[index + 1] & 0xff) << 16)
                | ((buffer[index + 2] & 0xff) << 8)
                | ((buffer[index + 3] & 0xff)));
    }

    public static void setLong(byte[] buffer, int index, long value) {
        buffer[index] = (byte) (value >>> 56);
        buffer[index + 1] = (byte) (value >>> 48);
        buffer[index + 2] = (byte) (value >>> 40);
        buffer[index + 3] = (byte) (value >>> 32);
        buffer[index + 4] = (byte) (value >>> 24);
        buffer[index + 5] = (byte) (value >>> 16);
        buffer[index + 6] = (byte) (value >>> 8);
        buffer[index + 7] = (byte) value;
    }

    public static long getLong(byte[] buffer, int index) {
        return (((buffer[index] & 0xffL) << 56)
                | ((buffer[index + 1] & 0xffL) << 48)
                | ((buffer[index + 2] & 0xffL) << 40)
                | ((buffer[index + 3] & 0xffL) << 32)
                | ((buffer[index + 4] & 0xffL) << 24)
                | ((buffer[index + 5] & 0xffL) << 16)
                | ((buffer[index + 6] & 0xffL) << 8)
                | ((buffer[index + 7] & 0xffL)));
    }
    // endregion

    // region 小端编码

    public static void setShortLE(byte[] buffer, int index, int value) {
        buffer[index] = (byte) value;
        buffer[index + 1] = (byte) (value >>> 8);
    }

    public static short getShortLE(byte[] buffer, int index) {
        return (short) ((buffer[index] & 0xff)
                | (buffer[index + 1] << 8));
    }

    public static void setIntLE(byte[] buffer, int index, int value) {
        buffer[index] = (byte) value;
        buffer[index + 1] = (byte) (value >>> 8);
        buffer[index + 2] = (byte) (value >>> 16);
        buffer[index + 3] = (byte) (value >>> 24);
    }

    public static int getIntLE(byte[] buffer, int index) {
        return (((buffer[index] & 0xff))
                | ((buffer[index + 1] & 0xff) << 8)
                | ((buffer[index + 2] & 0xff) << 16)
                | ((buffer[index + 3] & 0xff) << 24));
    }

    public static void setLongLE(byte[] buffer, int index, long value) {
        buffer[index] = (byte) value;
        buffer[index + 1] = (byte) (value >>> 8);
        buffer[index + 2] = (byte) (value >>> 16);
        buffer[index + 3] = (byte) (value >>> 24);
        buffer[index + 4] = (byte) (value >>> 32);
        buffer[index + 5] = (byte) (value >>> 40);
        buffer[index + 6] = (byte) (value >>> 48);
        buffer[index + 7] = (byte) (value >>> 56);
    }

    public static long getLongLE(byte[] buffer, int index) {
        return (((buffer[index] & 0xffL))
                | ((buffer[index + 1] & 0xffL) << 8)
                | ((buffer[index + 2] & 0xffL) << 16)
                | ((buffer[index + 3] & 0xffL) << 24)
                | ((buffer[index + 4] & 0xffL) << 32)
                | ((buffer[index + 5] & 0xffL) << 40)
                | ((buffer[index + 6] & 0xffL) << 48)
                | ((buffer[index + 7] & 0xffL) << 56));
    }

    // endregion

    // 以下参考自protobuf，以避免引入PB
    // region protobuf util

    private static final int IntCodedMask1 = (-1) << 7; // 低7位0
    private static final int IntCodedMask2 = (-1) << 14; // 低14位0
    private static final int IntCodedMask3 = (-1) << 21;
    private static final int IntCodedMask4 = (-1) << 28;

    private static final long LongCodedMask1 = (-1L) << 7;
    private static final long LongCodedMask2 = (-1L) << 14;
    private static final long LongCodedMask3 = (-1L) << 21;
    private static final long LongCodedMask4 = (-1L) << 28;
    private static final long LongCodedMask5 = (-1L) << 35;
    private static final long LongCodedMask6 = (-1L) << 42;
    private static final long LongCodedMask7 = (-1L) << 49;
    private static final long LongCodedMask8 = (-1L) << 56;
    private static final long LongCodedMask9 = (-1L) << 63;

    /** 计算原始的32位变长整形的编码长度 */
    public static int computeRawVarInt32Size(int value) {
        if ((value & IntCodedMask1) == 0) return 1; // 所有高位为0
        if ((value & IntCodedMask2) == 0) return 2;
        if ((value & IntCodedMask3) == 0) return 3;
        if ((value & IntCodedMask4) == 0) return 4;
        return 5;
    }

    /** 计算原始的64位变长整形的编码长度 */
    public static int computeRawVarInt64Size(long value) {
        if ((value & LongCodedMask1) == 0) return 1; // 所有高位为0
        if ((value & LongCodedMask2) == 0) return 2;
        if ((value & LongCodedMask3) == 0) return 3;
        if ((value & LongCodedMask4) == 0) return 4;
        if ((value & LongCodedMask5) == 0) return 5;
        if ((value & LongCodedMask6) == 0) return 6;
        if ((value & LongCodedMask7) == 0) return 7;
        if ((value & LongCodedMask8) == 0) return 8;
        if ((value & LongCodedMask9) == 0) return 9;
        return 10;
    }

    public static int encodeZigZag32(int n) {
        return (n << 1) ^ (n >> 31);
    }

    public static int decodeZigZag32(final int n) {
        return (n >>> 1) ^ -(n & 1);
    }

    public static long encodeZigZag64(long n) {
        return (n << 1) ^ (n >> 63);
    }

    public static long decodeZigZag64(final long n) {
        return (n >>> 1) ^ -(n & 1);
    }
    // endregion

    //region protobuf decode

    public static int readInt32(byte[] buffer, int pos, MutableInt newPos) {
        long rawBits = readRawVarint64(buffer, pos, newPos);
        return (int) rawBits;
    }

    public static long readInt64(byte[] buffer, int pos, MutableInt newPos) {
        return readRawVarint64(buffer, pos, newPos);
    }

    public static int readUint32(byte[] buffer, int pos, MutableInt newPos) {
        return (int) readRawVarint64(buffer, pos, newPos);
    }

    public static long readUint64(byte[] buffer, int pos, MutableInt newPos) {
        return readRawVarint64(buffer, pos, newPos);
    }

    public static int readSint32(byte[] buffer, int pos, MutableInt newPos) {
        long rawBits = readRawVarint64(buffer, pos, newPos);
        return decodeZigZag32((int) rawBits);
    }

    public static long readSint64(byte[] buffer, int pos, MutableInt newPos) {
        long rawBits = readRawVarint64(buffer, pos, newPos);
        return decodeZigZag64(rawBits);
    }

    public static int readFixed32(byte[] buffer, int pos, MutableInt newPos) {
        return readRawFixed32(buffer, pos, newPos);
    }

    public static long readFixed64(byte[] buffer, int pos, MutableInt newPos) {
        return readRawFixed64(buffer, pos, newPos);
    }

    public static float readFloat(byte[] buffer, int pos, MutableInt newPos) {
        int rawBits = readRawFixed32(buffer, pos, newPos);
        return Float.intBitsToFloat(rawBits);
    }

    public static double readDouble(byte[] buffer, int pos, MutableInt newPos) {
        long rawBits = readRawFixed64(buffer, pos, newPos);
        return Double.longBitsToDouble(rawBits);
    }

    /** varint编码不区分int和long，而是固定读取到高位字节为0，因此无需两个方法 */
    private static long readRawVarint64(byte[] buffer, int pos, MutableInt newPos) {
        // 单字节优化
        byte b = buffer[pos++];
        long r = (b & 127L);
        if ((b & 128) == 0) {
            newPos.setValue(pos);
            return r;
        }
        int shift = 7;
        do {
            b = buffer[pos++];
            r |= (b & 127L) << shift; // 取后7位左移
            if ((b & 128) == 0) { // 高位0
                newPos.setValue(pos);
                return r;
            }
            shift += 7;
        } while (shift < 64);
        // 读取超过10个字节
        throw new DsonIOException("DsonInput encountered a malformed varint.");
    }

    private static int readRawFixed32(byte[] buffer, int pos, MutableInt newPos) {
        int r = (((buffer[pos] & 0xff))
                | ((buffer[pos + 1] & 0xff) << 8)
                | ((buffer[pos + 2] & 0xff) << 16)
                | ((buffer[pos + 3] & 0xff) << 24));
        newPos.setValue(pos + 4);
        return r;
    }

    private static long readRawFixed64(byte[] buffer, int pos, MutableInt newPos) {
        long r = (((buffer[pos] & 0xffL))
                | ((buffer[pos + 1] & 0xffL) << 8)
                | ((buffer[pos + 2] & 0xffL) << 16)
                | ((buffer[pos + 3] & 0xffL) << 24)
                | ((buffer[pos + 4] & 0xffL) << 32)
                | ((buffer[pos + 5] & 0xffL) << 40)
                | ((buffer[pos + 6] & 0xffL) << 48)
                | ((buffer[pos + 7] & 0xffL) << 56));
        newPos.setValue(pos + 8);
        return r;
    }

    //endregion

    //region protobuf encode

    /** @return newPos */
    public static int writeInt32(byte[] buffer, int pos, int value) {
        if (value >= 0) {
            return writeRawVarint32(buffer, pos, value);
        } else {
            return writeRawVarint64(buffer, pos, value);
        }
    }

    public static int writeInt64(byte[] buffer, int pos, long value) {
        return writeRawVarint64(buffer, pos, value);
    }

    public static int writeUint32(byte[] buffer, int pos, int value) {
        return writeRawVarint32(buffer, pos, value);
    }

    public static int writeUint64(byte[] buffer, int pos, long value) {
        return writeRawVarint64(buffer, pos, value);
    }

    public static int writeSint32(byte[] buffer, int pos, int value) {
        return writeRawVarint32(buffer, pos, encodeZigZag32(value));
    }

    public static int writeSint64(byte[] buffer, int pos, long value) {
        return writeRawVarint64(buffer, pos, encodeZigZag64(value));
    }

    public static int writeFixed32(byte[] buffer, int pos, int value) {
        return writeRawFixed32(buffer, pos, value);
    }

    public static int writeFixed64(byte[] buffer, int pos, long value) {
        return writeRawFixed64(buffer, pos, value);
    }

    public static int writeFloat(byte[] buffer, int pos, float value) {
        return writeRawFixed32(buffer, pos, Float.floatToRawIntBits(value));
    }

    public static int writeDouble(byte[] buffer, int pos, double value) {
        return writeRawFixed64(buffer, pos, Double.doubleToRawLongBits(value));
    }

    /**
     * 写入一个变长的64位整数，所有的负数都将固定10字节
     *
     * @param buffer buffer
     * @param pos    开始写入的位置
     * @param value  要写入的值
     * @return 写入后的新坐标
     */
    private static int writeRawVarint64(byte[] buffer, int pos, long value) {
        if (value >= 0 && value < 128L) { // 小数值较多的情况下有意义
            buffer[pos] = (byte) value;
            return pos + 1;
        }
        while (true) {
            if ((value & LongCodedMask1) != 0) {
                buffer[pos++] = (byte) ((value & 127L) | 128L); // 截取后7位，高位补1
                value >>>= 7; // java必须逻辑右移
            } else {
                buffer[pos++] = (byte) value;
                return pos;
            }
        }
    }

    /**
     * 写入一个变长的32位整数，负数将固定为5字节
     * （注意：普通int慎重调用，这里将int看做无符号整数编码）
     *
     * @param buffer buffer
     * @param pos    开始写入的位置
     * @param value  要写入的值
     * @return 写入后的新坐标
     */
    private static int writeRawVarint32(byte[] buffer, int pos, int value) {
        if (value >= 0 && value < 128) { // 小数值较多的情况下有意义
            buffer[pos] = (byte) value;
            return pos + 1;
        }
        while (true) {
            if ((value & IntCodedMask1) != 0) {
                buffer[pos++] = (byte) ((value & 127) | 128); // 截取后7位，高位补1
                value >>>= 7; // java必须逻辑右移
            } else {
                buffer[pos++] = (byte) value;
                return pos;
            }
        }
    }

    private static int writeRawFixed32(byte[] buffer, int pos, int value) {
        buffer[pos] = (byte) value;
        buffer[pos + 1] = (byte) (value >> 8);
        buffer[pos + 2] = (byte) (value >> 16);
        buffer[pos + 3] = (byte) (value >> 24);
        return pos + 4;
    }

    private static int writeRawFixed64(byte[] buffer, int pos, long value) {
        buffer[pos] = (byte) value;
        buffer[pos + 1] = (byte) (value >> 8);
        buffer[pos + 2] = (byte) (value >> 16);
        buffer[pos + 3] = (byte) (value >> 24);
        buffer[pos + 4] = (byte) (value >> 32);
        buffer[pos + 5] = (byte) (value >> 40);
        buffer[pos + 6] = (byte) (value >> 48);
        buffer[pos + 7] = (byte) (value >> 56);
        return pos + 8;
    }

    //endregion
}