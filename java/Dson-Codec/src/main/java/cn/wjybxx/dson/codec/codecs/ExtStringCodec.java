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

import cn.wjybxx.dson.codec.DuplexCodec;
import cn.wjybxx.dson.codec.TypeArgInfo;
import cn.wjybxx.dson.codec.dson.DsonCodecScanIgnore;
import cn.wjybxx.dson.codec.dson.DsonObjectReader;
import cn.wjybxx.dson.codec.dson.DsonObjectWriter;
import cn.wjybxx.dson.codec.dsonlite.DsonLiteCodecScanIgnore;
import cn.wjybxx.dson.codec.dsonlite.DsonLiteObjectReader;
import cn.wjybxx.dson.codec.dsonlite.DsonLiteObjectWriter;
import cn.wjybxx.dson.text.ObjectStyle;
import cn.wjybxx.dson.text.StringStyle;
import cn.wjybxx.dson.types.ExtString;

import javax.annotation.Nonnull;

/**
 * @author wjybxx
 * date - 2023/12/28
 */
@DsonLiteCodecScanIgnore
@DsonCodecScanIgnore
public class ExtStringCodec implements DuplexCodec<ExtString> {

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
    public Class<ExtString> getEncoderClass() {
        return ExtString.class;
    }

    @Override
    public void writeObject(DsonLiteObjectWriter writer, ExtString instance, TypeArgInfo<?> typeArgInfo) {
        // 外部writeName
        writer.writeExtString(writer.getCurrentName(), instance);
    }

    @Override
    public ExtString readObject(DsonLiteObjectReader reader, TypeArgInfo<?> typeArgInfo) {
        // 外部已readName
        return reader.readExtString(reader.getCurrentName());
    }

    @Override
    public void writeObject(DsonObjectWriter writer, ExtString instance, TypeArgInfo<?> typeArgInfo, ObjectStyle style) {
        writer.writeExtString(writer.getCurrentName(), instance, StringStyle.AUTO);
    }

    @Override
    public ExtString readObject(DsonObjectReader reader, TypeArgInfo<?> typeArgInfo) {
        return reader.readExtString(reader.getCurrentName());
    }
}
