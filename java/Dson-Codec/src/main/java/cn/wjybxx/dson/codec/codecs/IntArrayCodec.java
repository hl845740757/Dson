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
import it.unimi.dsi.fastutil.ints.IntArrayList;

import javax.annotation.Nonnull;

/**
 * @author wjybxx
 * date 2023/4/4
 */
@DsonLiteCodecScanIgnore
@DsonCodecScanIgnore
public class IntArrayCodec implements DuplexCodec<int[]> {

    @Nonnull
    @Override
    public Class<int[]> getEncoderClass() {
        return int[].class;
    }

    @Override
    public void writeObject(DsonLiteObjectWriter writer, int[] instance, TypeArgInfo<?> typeArgInfo) {
        for (int e : instance) {
            writer.writeInt(0, e);
        }
    }

    @Override
    public int[] readObject(DsonLiteObjectReader reader, TypeArgInfo<?> typeArgInfo) {
        IntArrayList result = new IntArrayList();
        while (reader.readDsonType() != DsonType.END_OF_OBJECT) {
            result.add(reader.readInt(0));
        }
        return result.toIntArray();
    }

    @Override
    public void writeObject(DsonObjectWriter writer, int[] instance, TypeArgInfo<?> typeArgInfo, ObjectStyle style) {
        for (int e : instance) {
            writer.writeInt(null, e);
        }
    }

    @Override
    public int[] readObject(DsonObjectReader reader, TypeArgInfo<?> typeArgInfo) {
        IntArrayList result = new IntArrayList();
        while (reader.readDsonType() != DsonType.END_OF_OBJECT) {
            result.add(reader.readInt(null));
        }
        return result.toIntArray();
    }
}