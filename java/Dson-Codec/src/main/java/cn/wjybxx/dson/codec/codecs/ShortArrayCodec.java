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

package cn.wjybxx.dson.codec.codecs;

import cn.wjybxx.dson.DsonType;
import cn.wjybxx.dson.codec.DuplexCodec;
import cn.wjybxx.dson.codec.TypeArgInfo;
import cn.wjybxx.dson.codec.dson.DsonCodecScanIgnore;
import cn.wjybxx.dson.codec.dson.DsonObjectReader;
import cn.wjybxx.dson.codec.dson.DsonObjectWriter;
import cn.wjybxx.dson.codec.dsonlite.DsonLiteCodecScanIgnore;
import cn.wjybxx.dson.codec.dsonlite.DsonLiteObjectReader;
import cn.wjybxx.dson.codec.dsonlite.DsonLiteObjectWriter;
import cn.wjybxx.dson.text.ObjectStyle;
import it.unimi.dsi.fastutil.shorts.ShortArrayList;

import javax.annotation.Nonnull;

/**
 * @author wjybxx
 * date 2023/4/4
 */
@DsonLiteCodecScanIgnore
@DsonCodecScanIgnore
public class ShortArrayCodec implements DuplexCodec<short[]> {

    @Nonnull
    @Override
    public Class<short[]> getEncoderClass() {
        return short[].class;
    }

    @Override
    public void writeObject(DsonLiteObjectWriter writer, short[] instance, TypeArgInfo<?> typeArgInfo) {
        for (short e : instance) {
            writer.writeShort(0, e);
        }
    }

    @Override
    public short[] readObject(DsonLiteObjectReader reader, TypeArgInfo<?> typeArgInfo) {
        ShortArrayList result = new ShortArrayList();
        while (reader.readDsonType() != DsonType.END_OF_OBJECT) {
            result.add(reader.readShort(0));
        }
        return result.toShortArray();
    }

    @Override
    public void writeObject(DsonObjectWriter writer, short[] instance, TypeArgInfo<?> typeArgInfo, ObjectStyle style) {
        for (short e : instance) {
            writer.writeShort(null, e);
        }
    }

    @Override
    public short[] readObject(DsonObjectReader reader, TypeArgInfo<?> typeArgInfo) {
        ShortArrayList result = new ShortArrayList();
        while (reader.readDsonType() != DsonType.END_OF_OBJECT) {
            result.add(reader.readShort(null));
        }
        return result.toShortArray();
    }
}