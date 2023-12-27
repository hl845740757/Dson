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

import cn.wjybxx.dson.DsonType;
import cn.wjybxx.dson.codec.PojoCodecImpl;
import cn.wjybxx.dson.codec.TypeArgInfo;
import cn.wjybxx.dson.codec.binary.BinaryObjectReader;
import cn.wjybxx.dson.codec.binary.BinaryObjectWriter;
import cn.wjybxx.dson.codec.binary.BinaryPojoCodecScanIgnore;
import cn.wjybxx.dson.codec.document.DocumentObjectReader;
import cn.wjybxx.dson.codec.document.DocumentObjectWriter;
import cn.wjybxx.dson.codec.document.DocumentPojoCodecScanIgnore;
import cn.wjybxx.dson.text.ObjectStyle;
import cn.wjybxx.dson.text.StringStyle;
import org.apache.commons.lang3.ArrayUtils;

import javax.annotation.Nonnull;
import java.util.ArrayList;

/**
 * @author wjybxx
 * date 2023/4/4
 */
@BinaryPojoCodecScanIgnore
@DocumentPojoCodecScanIgnore
public class StringArrayCodec implements PojoCodecImpl<String[]> {

    @Nonnull
    @Override
    public Class<String[]> getEncoderClass() {
        return String[].class;
    }

    @Override
    public void writeObject(BinaryObjectWriter writer, String[] instance, TypeArgInfo<?> typeArgInfo) {
        for (String e : instance) {
            writer.writeString(0, e);
        }
    }

    @Override
    public String[] readObject(BinaryObjectReader reader, TypeArgInfo<?> typeArgInfo) {
        ArrayList<String> result = new ArrayList<>();
        while (reader.readDsonType() != DsonType.END_OF_OBJECT) {
            result.add(reader.readString(0));
        }
        return result.toArray(ArrayUtils.EMPTY_STRING_ARRAY);
    }

    @Override
    public void writeObject(DocumentObjectWriter writer, String[] instance, TypeArgInfo<?> typeArgInfo, ObjectStyle style) {
        for (String e : instance) {
            writer.writeString(null, e, StringStyle.AUTO);
        }
    }

    @Override
    public String[] readObject(DocumentObjectReader reader, TypeArgInfo<?> typeArgInfo) {
        ArrayList<String> result = new ArrayList<>();
        while (reader.readDsonType() != DsonType.END_OF_OBJECT) {
            result.add(reader.readString(null));
        }
        return result.toArray(ArrayUtils.EMPTY_STRING_ARRAY);
    }
}