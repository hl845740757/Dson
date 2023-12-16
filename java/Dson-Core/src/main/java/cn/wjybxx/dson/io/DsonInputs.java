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

import javax.annotation.Nonnull;
import java.nio.charset.StandardCharsets;

/**
 * 核心包去除了对Protobuf的支持，如果期望使用protobuf和netty读取数据，可引入相应的扩展包。
 *
 * @author wjybxx
 * date - 2023/4/22
 */
public class DsonInputs {

    public static DsonInput newInstance(@Nonnull byte[] buffer) {
        return new ArrayDsonInput(buffer, 0, buffer.length);
    }

    public static DsonInput newInstance(byte[] buffer, int offset, int length) {
        return new ArrayDsonInput(buffer, offset, length);
    }

    static class ArrayDsonInput implements DsonInput {

        private final byte[] buffer;
        private final int rawOffset;
        private final int rawLimit;

        private int bufferPos;
        private int bufferPosLimit;
        private final MutableInt newPos = new MutableInt();

        ArrayDsonInput(byte[] buffer, int offset, int length) {
            BinaryUtils.checkBuffer(buffer, offset, length);
            this.buffer = buffer;
            this.rawOffset = offset;
            this.rawLimit = offset + length;

            this.bufferPos = offset;
            this.bufferPosLimit = offset + length;
        }

        // region check

        private int checkNewBufferPos(int newBufferPos) {
            if (newBufferPos < rawOffset || newBufferPos > bufferPosLimit) {
                throw new DsonIOException("BytesLimited, LimitPos: %d, position: %d, newPosition: %d"
                        .formatted(bufferPosLimit, bufferPos, newBufferPos));
            }
            return newBufferPos;
        }

        //endregion

        // region basic

        @Override
        public byte readRawByte() {
            checkNewBufferPos(bufferPos + 1);
            return buffer[bufferPos++];
        }

        @Override
        public int readInt32() {
            try {
                int r = BinaryUtils.readInt32(buffer, bufferPos, newPos);
                bufferPos = checkNewBufferPos(newPos.intValue());
                return r;
            } catch (Exception e) {
                throw DsonIOException.wrap(e, "buffer overflow");
            }
        }

        @Override
        public int readUint32() {
            try {
                int r = BinaryUtils.readUint32(buffer, bufferPos, newPos);
                bufferPos = checkNewBufferPos(newPos.intValue());
                return r;
            } catch (Exception e) {
                throw DsonIOException.wrap(e, "buffer overflow");
            }
        }

        @Override
        public int readSint32() {
            try {
                int r = BinaryUtils.readSint32(buffer, bufferPos, newPos);
                bufferPos = checkNewBufferPos(newPos.intValue());
                return r;
            } catch (Exception e) {
                throw DsonIOException.wrap(e, "buffer overflow");
            }
        }

        @Override
        public int readFixed32() {
            try {
                int r = BinaryUtils.readFixed32(buffer, bufferPos, newPos);
                bufferPos = checkNewBufferPos(newPos.intValue());
                return r;
            } catch (Exception e) {
                throw DsonIOException.wrap(e, "buffer overflow");
            }
        }

        @Override
        public long readInt64() {
            try {
                long r = BinaryUtils.readInt64(buffer, bufferPos, newPos);
                bufferPos = checkNewBufferPos(newPos.intValue());
                return r;
            } catch (Exception e) {
                throw DsonIOException.wrap(e, "buffer overflow");
            }
        }

        @Override
        public long readUint64() {
            try {
                long r = BinaryUtils.readUint64(buffer, bufferPos, newPos);
                bufferPos = checkNewBufferPos(newPos.intValue());
                return r;
            } catch (Exception e) {
                throw DsonIOException.wrap(e, "buffer overflow");
            }
        }

        @Override
        public long readSint64() {
            try {
                long r = BinaryUtils.readSint64(buffer, bufferPos, newPos);
                bufferPos = checkNewBufferPos(newPos.intValue());
                return r;
            } catch (Exception e) {
                throw DsonIOException.wrap(e, "buffer overflow");
            }
        }

        @Override
        public long readFixed64() {
            try {
                long r = BinaryUtils.readFixed64(buffer, bufferPos, newPos);
                bufferPos = checkNewBufferPos(newPos.intValue());
                return r;
            } catch (Exception e) {
                throw DsonIOException.wrap(e, "buffer overflow");
            }
        }

        @Override
        public float readFloat() {
            try {
                float r = BinaryUtils.readFloat(buffer, bufferPos, newPos);
                bufferPos = checkNewBufferPos(newPos.intValue());
                return r;
            } catch (Exception e) {
                throw DsonIOException.wrap(e, "buffer overflow");
            }
        }

        @Override
        public double readDouble() {
            try {
                double r = BinaryUtils.readDouble(buffer, bufferPos, newPos);
                bufferPos = checkNewBufferPos(newPos.intValue());
                return r;
            } catch (Exception e) {
                throw DsonIOException.wrap(e, "buffer overflow");
            }
        }

        @Override
        public boolean readBool() {
            checkNewBufferPos(bufferPos + 1);
            return buffer[bufferPos++] != 0;
        }

        @Override
        public String readString() {
            try {
                int len = BinaryUtils.readUint32(buffer, bufferPos, newPos); // 字符串长度
                checkNewBufferPos(newPos.intValue() + len); // 先检查，避免构建无效字符串

                String r = new String(buffer, newPos.intValue(), len, StandardCharsets.UTF_8);
                bufferPos = newPos.intValue() + len;
                return r;
            } catch (Exception e) {
                throw DsonIOException.wrap(e, "buffer overflow");
            }
        }

        @Override
        public byte[] readRawBytes(int count) {
            checkNewBufferPos(bufferPos + count);
            byte[] bytes = new byte[count];
            System.arraycopy(buffer, bufferPos, bytes, 0, count);
            bufferPos += count;
            return bytes;
        }

        @Override
        public void skipRawBytes(int n) {
            if (n < 0) throw new IllegalArgumentException("n");
            if (n == 0) return;
            bufferPos = checkNewBufferPos(bufferPos + n);
        }
        // endregion

        //region sp

        @Override
        public int getPosition() {
            return bufferPos - rawOffset;
        }

        @Override
        public void setPosition(int value) {
            BinaryUtils.checkBuffer(rawLimit - rawOffset, value);
            bufferPos = rawOffset + value;
        }

        @Override
        public byte getByte(int pos) {
            BinaryUtils.checkBuffer(rawLimit - rawOffset, pos, 1);
            int bufferPos = rawOffset + pos;
            return buffer[bufferPos];
        }

        @Override
        public int getFixed32(int pos) {
            BinaryUtils.checkBuffer(rawLimit - rawOffset, pos, 4);
            int bufferPos = rawOffset + pos;
            return BinaryUtils.getIntLE(buffer, bufferPos);
        }

        @Override
        public int pushLimit(int byteLimit) {
            if (byteLimit < 0) throw new IllegalArgumentException("byteLimit");
            int oldPosLimit = bufferPosLimit;
            int newPosLimit = bufferPos + byteLimit;

            // 不可超过原始限制
            BinaryUtils.checkBuffer(rawLimit, rawOffset, newPosLimit - rawOffset);
            bufferPosLimit = newPosLimit;
            return oldPosLimit;
        }

        @Override
        public void popLimit(int oldLimit) {
            // 不可超过原始限制
            BinaryUtils.checkBuffer(rawLimit, rawOffset, oldLimit - rawOffset);
            bufferPosLimit = oldLimit;
        }

        @Override
        public int getBytesUntilLimit() {
            return (bufferPosLimit - bufferPos);
        }

        @Override
        public boolean isAtEnd() {
            return bufferPos >= bufferPosLimit;
        }

        @Override
        public void close() {

        }
    }
}