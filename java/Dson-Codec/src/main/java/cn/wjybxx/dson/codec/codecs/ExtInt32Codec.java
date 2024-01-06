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
import cn.wjybxx.dson.codec.DuplexCodec;
import cn.wjybxx.dson.codec.TypeArgInfo;
import cn.wjybxx.dson.codec.dson.DsonObjectReader;
import cn.wjybxx.dson.codec.dson.DsonObjectWriter;
import cn.wjybxx.dson.codec.dsonlite.DsonLiteObjectReader;
import cn.wjybxx.dson.codec.dsonlite.DsonLiteObjectWriter;
import cn.wjybxx.dson.text.NumberStyle;
import cn.wjybxx.dson.text.ObjectStyle;
import cn.wjybxx.dson.types.ExtInt32;

import javax.annotation.Nonnull;

/**
 * @author wjybxx
 * date - 2023/12/28
 */
public class ExtInt32Codec implements DuplexCodec<ExtInt32> {

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
    public Class<ExtInt32> getEncoderClass() {
        return ExtInt32.class;
    }

    @Override
    public void writeObject(DsonLiteObjectWriter writer, ExtInt32 instance, TypeArgInfo<?> typeArgInfo) {
        // 外部writeName
        writer.writeExtInt32(writer.getCurrentName(), instance, WireType.VARINT);
    }

    @Override
    public ExtInt32 readObject(DsonLiteObjectReader reader, TypeArgInfo<?> typeArgInfo) {
        // 外部已readName
        return reader.readExtInt32(reader.getCurrentName());
    }

    @Override
    public void writeObject(DsonObjectWriter writer, ExtInt32 instance, TypeArgInfo<?> typeArgInfo, ObjectStyle style) {
        writer.writeExtInt32(writer.getCurrentName(), instance, WireType.VARINT, NumberStyle.SIMPLE);
    }

    @Override
    public ExtInt32 readObject(DsonObjectReader reader, TypeArgInfo<?> typeArgInfo) {
        return reader.readExtInt32(reader.getCurrentName());
    }
}
