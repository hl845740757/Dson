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
import cn.wjybxx.dson.codec.document.DocumentObjectReader;
import cn.wjybxx.dson.codec.document.DocumentObjectWriter;
import cn.wjybxx.dson.text.ObjectStyle;
import cn.wjybxx.dson.types.Binary;

import javax.annotation.Nonnull;

/**
 * @author houlei
 * date - 2023/12/28
 */
public class BinaryCodec implements PojoCodecImpl<Binary> {

    @Override
    public boolean isWriteAsArray() {
        return false;
    }

    @Override
    public boolean autoStartEnd() {
        return false;
    }

    @Nonnull
    @Override
    public Class<Binary> getEncoderClass() {
        return Binary.class;
    }

    @Override
    public void writeObject(BinaryObjectWriter writer, Binary instance, TypeArgInfo<?> typeArgInfo) {
        // 外部writeName
        writer.writeBinary(writer.getCurrentName(), instance);
    }

    @Override
    public Binary readObject(BinaryObjectReader reader, TypeArgInfo<?> typeArgInfo) {
        // 外部已readName
        return reader.readBinary(reader.getCurrentName());
    }

    @Override
    public void writeObject(DocumentObjectWriter writer, Binary instance, TypeArgInfo<?> typeArgInfo, ObjectStyle style) {
        writer.writeBinary(writer.getCurrentName(), instance);
    }

    @Override
    public Binary readObject(DocumentObjectReader reader, TypeArgInfo<?> typeArgInfo) {
        return reader.readBinary(reader.getCurrentName());
    }
}
