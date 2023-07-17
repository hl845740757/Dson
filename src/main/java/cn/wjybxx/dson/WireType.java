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

import cn.wjybxx.dson.io.DsonInput;
import cn.wjybxx.dson.io.DsonOutput;

import java.util.List;

/**
 * 数字类型字段的编码方式
 *
 * @author wjybxx
 * date - 2023/4/19
 */
public enum WireType {

    /**
     * 简单变长编码
     * 1.该编码对于int32的负数数据而言，将固定占用10个字节，正数时等同于UINT编码；
     * 1.该编码对于int64的负数数据而言，也固定占用10个字节，正数时等同于UINT编码；
     */
    VARINT(0) {
        @Override
        public void writeInt32(DsonOutput output, int value) {
            output.writeInt32(value);
        }

        @Override
        public int readInt32(DsonInput input) {
            return input.readInt32();
        }

        @Override
        public void writeInt64(DsonOutput output, long value) {
            output.writeInt64(value);
        }

        @Override
        public long readInt64(DsonInput input) {
            return input.readInt64();
        }
    },

    /**
     * 按照无符号格式优化编码
     * 1.该编码对于int32的负数数据而言，将固定占用5个字节；
     * 1.该编码对于int64的负数数据而言，将固定占用10个字节；
     */
    UINT(1) {
        @Override
        public void writeInt32(DsonOutput output, int value) {
            output.writeUint32(value);
        }

        @Override
        public int readInt32(DsonInput input) {
            return input.readUint32();
        }

        @Override
        public void writeInt64(DsonOutput output, long value) {
            output.writeUint64(value);
        }

        @Override
        public long readInt64(DsonInput input) {
            return input.readUint64();
        }
    },

    /**
     * 按照有符号数格式优化编码
     */
    SINT(2) {
        @Override
        public void writeInt32(DsonOutput output, int value) {
            output.writeSint32(value);
        }

        @Override
        public int readInt32(DsonInput input) {
            return input.readSint32();
        }

        @Override
        public void writeInt64(DsonOutput output, long value) {
            output.writeSint64(value);
        }

        @Override
        public long readInt64(DsonInput input) {
            return input.readSint64();
        }
    },

    /** 固定长度编码 */
    FIXED(3) {
        @Override
        public void writeInt32(DsonOutput output, int value) {
            output.writeFixed32(value);
        }

        @Override
        public int readInt32(DsonInput input) {
            return input.readFixed32();
        }

        @Override
        public void writeInt64(DsonOutput output, long value) {
            output.writeFixed64(value);
        }

        @Override
        public long readInt64(DsonInput input) {
            return input.readFixed64();
        }
    },

    /** 按照8位有符号整数编码 */
    BYTE(4) {
        @Override
        public void writeInt32(DsonOutput output, int value) {
            output.writeRawByte((byte) value);
        }

        @Override
        public int readInt32(DsonInput input) {
            return input.readRawByte();
        }

        @Override
        public void writeInt64(DsonOutput output, long value) {
            output.writeRawByte((byte) value);
        }

        @Override
        public long readInt64(DsonInput input) {
            return input.readRawByte();
        }
    };

    private final int number;

    WireType(int number) {
        this.number = number;
    }

    public int getNumber() {
        return number;
    }

    public static final List<WireType> LOOK_TABLE = List.of(values());

    public static WireType forNumber(int number) {
        return switch (number) {
            case 0 -> VARINT;
            case 1 -> UINT;
            case 2 -> SINT;
            case 3 -> FIXED;
            case 4 -> BYTE;
            default -> throw new IllegalArgumentException("invalid wireType " + number);
        };
    }

    public abstract void writeInt32(DsonOutput output, int value);

    public abstract int readInt32(DsonInput input);

    public abstract void writeInt64(DsonOutput output, long value);

    public abstract long readInt64(DsonInput input);

}