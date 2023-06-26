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

package cn.wjybxx.dson.codec.document.codecs;

import cn.wjybxx.dson.DsonType;
import cn.wjybxx.dson.codec.TypeArgInfo;
import cn.wjybxx.dson.codec.document.DocumentObjectReader;
import cn.wjybxx.dson.codec.document.DocumentObjectWriter;
import cn.wjybxx.dson.codec.document.DocumentPojoCodecImpl;
import cn.wjybxx.dson.codec.document.DocumentPojoCodecScanIgnore;
import it.unimi.dsi.fastutil.floats.FloatArrayList;

import javax.annotation.Nonnull;

/**
 * @author wjybxx
 * date 2023/4/4
 */
@DocumentPojoCodecScanIgnore
public class FloatArrayCodec implements DocumentPojoCodecImpl<float[]> {

    @Nonnull
    @Override
    public String getTypeName() {
        return "float[]";
    }

    @Nonnull
    @Override
    public Class<float[]> getEncoderClass() {
        return float[].class;
    }

    @Override
    public void writeObject(float[] instance, DocumentObjectWriter writer, TypeArgInfo<?> typeArgInfo) {
        for (float e : instance) {
            writer.writeFloat(null, e);
        }
    }

    @Override
    public float[] readObject(DocumentObjectReader reader, TypeArgInfo<?> typeArgInfo) {
        FloatArrayList result = new FloatArrayList();
        while (reader.readDsonType() != DsonType.END_OF_OBJECT) {
            result.add(reader.readFloat(null));
        }
        return result.toFloatArray();
    }
}