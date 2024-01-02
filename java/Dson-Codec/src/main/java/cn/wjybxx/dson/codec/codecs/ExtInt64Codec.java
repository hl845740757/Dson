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

import cn.wjybxx.dson.WireType;
import cn.wjybxx.dson.codec.PojoCodecImpl;
import cn.wjybxx.dson.codec.TypeArgInfo;
import cn.wjybxx.dson.codec.binary.BinaryObjectReader;
import cn.wjybxx.dson.codec.binary.BinaryObjectWriter;
import cn.wjybxx.dson.codec.document.DocumentObjectReader;
import cn.wjybxx.dson.codec.document.DocumentObjectWriter;
import cn.wjybxx.dson.text.NumberStyle;
import cn.wjybxx.dson.text.ObjectStyle;
import cn.wjybxx.dson.types.ExtInt64;

import javax.annotation.Nonnull;

/**
 * @author wjybxx
 * date - 2023/12/28
 */
public class ExtInt64Codec implements PojoCodecImpl<ExtInt64> {

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
    public Class<ExtInt64> getEncoderClass() {
        return ExtInt64.class;
    }

    @Override
    public void writeObject(BinaryObjectWriter writer, ExtInt64 instance, TypeArgInfo<?> typeArgInfo) {
        // 外部writeName
        writer.writeExtInt64(writer.getCurrentName(), instance, WireType.VARINT);
    }

    @Override
    public ExtInt64 readObject(BinaryObjectReader reader, TypeArgInfo<?> typeArgInfo) {
        // 外部已readName
        return reader.readExtInt64(reader.getCurrentName());
    }

    @Override
    public void writeObject(DocumentObjectWriter writer, ExtInt64 instance, TypeArgInfo<?> typeArgInfo, ObjectStyle style) {
        writer.writeExtInt64(writer.getCurrentName(), instance, WireType.VARINT, NumberStyle.SIMPLE);
    }

    @Override
    public ExtInt64 readObject(DocumentObjectReader reader, TypeArgInfo<?> typeArgInfo) {
        return reader.readExtInt64(reader.getCurrentName());
    }
}
