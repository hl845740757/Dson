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

package cn.wjybxx.dson.codec.codecs;

import cn.wjybxx.dson.codec.PojoCodecImpl;
import cn.wjybxx.dson.codec.TypeArgInfo;
import cn.wjybxx.dson.codec.binary.BinaryObjectReader;
import cn.wjybxx.dson.codec.binary.BinaryObjectWriter;
import cn.wjybxx.dson.codec.binary.BinaryPojoCodecScanIgnore;
import cn.wjybxx.dson.codec.document.DocumentObjectReader;
import cn.wjybxx.dson.codec.document.DocumentObjectWriter;
import cn.wjybxx.dson.codec.document.DocumentPojoCodecScanIgnore;
import cn.wjybxx.dson.DsonType;
import cn.wjybxx.dson.text.ObjectStyle;
import it.unimi.dsi.fastutil.shorts.ShortArrayList;

import javax.annotation.Nonnull;

/**
 * @author wjybxx
 * date 2023/4/4
 */
@BinaryPojoCodecScanIgnore
@DocumentPojoCodecScanIgnore
public class ShortArrayCodec implements PojoCodecImpl<short[]> {

    @Nonnull
    @Override
    public Class<short[]> getEncoderClass() {
        return short[].class;
    }

    @Override
    public void writeObject(BinaryObjectWriter writer, short[] instance, TypeArgInfo<?> typeArgInfo) {
        for (short e : instance) {
            writer.writeShort(0, e);
        }
    }

    @Override
    public short[] readObject(BinaryObjectReader reader, TypeArgInfo<?> typeArgInfo) {
        ShortArrayList result = new ShortArrayList();
        while (reader.readDsonType() != DsonType.END_OF_OBJECT) {
            result.add(reader.readShort(0));
        }
        return result.toShortArray();
    }

    @Override
    public void writeObject(DocumentObjectWriter writer, short[] instance, TypeArgInfo<?> typeArgInfo, ObjectStyle style) {
        for (short e : instance) {
            writer.writeShort(null, e);
        }
    }

    @Override
    public short[] readObject(DocumentObjectReader reader, TypeArgInfo<?> typeArgInfo) {
        ShortArrayList result = new ShortArrayList();
        while (reader.readDsonType() != DsonType.END_OF_OBJECT) {
            result.add(reader.readShort(null));
        }
        return result.toShortArray();
    }
}