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

package cn.wjybxx.dson.types;

import cn.wjybxx.dson.Dsons;

import java.util.Arrays;
import java.util.HexFormat;

/**
 * 你通常不应该修改data中的数据。
 * 该类难以实现不可变对象，虽然我们可以封装为ByteArray，
 * 但许多接口都是基于byte[]的，封装会导致难以与其它接口协作。
 *
 * @author wjybxx
 * date - 2023/4/19
 */
public class Binary {

    private final int type;
    private final byte[] data;

    public Binary(byte[] data) {
        this(0, data);
    }

    public Binary(int type, byte[] data) {
        Dsons.checkSubType(type);
        Dsons.checkBinaryLength(data.length);
        this.type = type;
        this.data = data;
    }

    /** 默认采取拷贝的方式，保证安全性 */
    public Binary(Binary src) {
        this.type = src.type;
        this.data = src.data.clone();
    }

    public int getType() {
        return type;
    }

    /** 最好不要持有data的引用，否则可能由于共享数据产生错误 */
    public byte[] getData() {
        return data;
    }

    /** 创建一个拷贝 */
    public Binary copy() {
        return new Binary(type, data.clone());
    }

    //region equals

    @Override
    public boolean equals(Object o) {
        if (this == o) return true;
        if (o == null || getClass() != o.getClass()) return false;

        Binary that = (Binary) o;

        if (type != that.type) return false;
        return Arrays.equals(data, that.data);
    }

    @Override
    public int hashCode() {
        int result = type;
        result = 31 * result + Arrays.hashCode(data);
        return result;
    }

    // endregion

    @Override
    public String toString() {
        return "Binary{" +
                "type=" + type +
                ", data=" + HexFormat.of().formatHex(data) +
                '}';
    }

}