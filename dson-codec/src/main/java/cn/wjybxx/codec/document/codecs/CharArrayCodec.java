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

package cn.wjybxx.codec.document.codecs;

import cn.wjybxx.dson.DsonType;
import cn.wjybxx.codec.TypeArgInfo;
import cn.wjybxx.codec.document.DocumentObjectReader;
import cn.wjybxx.codec.document.DocumentObjectWriter;
import cn.wjybxx.codec.document.DocumentPojoCodecImpl;
import cn.wjybxx.codec.document.DocumentPojoCodecScanIgnore;
import it.unimi.dsi.fastutil.chars.CharArrayList;

import javax.annotation.Nonnull;

/**
 * @author wjybxx
 * date 2023/4/4
 */
@DocumentPojoCodecScanIgnore
public class CharArrayCodec implements DocumentPojoCodecImpl<char[]> {

    @Nonnull
    @Override
    public String getTypeName() {
        return "char[]";
    }

    @Nonnull
    @Override
    public Class<char[]> getEncoderClass() {
        return char[].class;
    }

    @Override
    public void writeObject(char[] instance, DocumentObjectWriter writer, TypeArgInfo<?> typeArgInfo) {
        for (char e : instance) {
            writer.writeChar(null, e);
        }
    }

    @Override
    public char[] readObject(DocumentObjectReader reader, TypeArgInfo<?> typeArgInfo) {
        CharArrayList result = new CharArrayList();
        while (reader.readDsonType() != DsonType.END_OF_OBJECT) {
            result.add(reader.readChar(null));
        }
        return result.toCharArray();
    }
}