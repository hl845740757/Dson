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

package cn.wjybxx.codec.binary.codecs;

import cn.wjybxx.codec.ConverterUtils;
import cn.wjybxx.codec.TypeArgInfo;
import cn.wjybxx.codec.binary.BinaryObjectReader;
import cn.wjybxx.codec.binary.BinaryObjectWriter;
import cn.wjybxx.codec.binary.BinaryPojoCodecImpl;
import cn.wjybxx.codec.binary.BinaryPojoCodecScanIgnore;
import cn.wjybxx.dson.DsonType;

import javax.annotation.Nonnull;
import java.util.ArrayList;

/**
 * @author wjybxx
 * date 2023/4/4
 */
@BinaryPojoCodecScanIgnore
public class BooleanArrayCodec implements BinaryPojoCodecImpl<boolean[]> {

    @Nonnull
    @Override
    public Class<boolean[]> getEncoderClass() {
        return boolean[].class;
    }

    @Override
    public void writeObject(boolean[] instance, BinaryObjectWriter writer, TypeArgInfo<?> typeArgInfo) {
        for (boolean e : instance) {
            writer.writeBoolean(0, e);
        }
    }

    @Override
    public boolean[] readObject(BinaryObjectReader reader, TypeArgInfo<?> typeArgInfo) {
        ArrayList<Boolean> result = new ArrayList<>();
        while (reader.readDsonType() != DsonType.END_OF_OBJECT) {
            result.add(reader.readBoolean(0));
        }
        return ConverterUtils.convertList2Array(result, boolean[].class);
    }
}