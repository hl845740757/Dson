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
import cn.wjybxx.dson.codec.ConverterUtils;
import cn.wjybxx.dson.codec.DuplexCodec;
import cn.wjybxx.dson.codec.TypeArgInfo;
import cn.wjybxx.dson.codec.dson.DsonCodecScanIgnore;
import cn.wjybxx.dson.codec.dson.DsonObjectReader;
import cn.wjybxx.dson.codec.dson.DsonObjectWriter;
import cn.wjybxx.dson.codec.dsonlite.DsonLiteCodecScanIgnore;
import cn.wjybxx.dson.codec.dsonlite.DsonLiteObjectReader;
import cn.wjybxx.dson.codec.dsonlite.DsonLiteObjectWriter;
import cn.wjybxx.dson.text.ObjectStyle;

import javax.annotation.Nonnull;
import java.util.ArrayList;

/**
 * @author wjybxx
 * date 2023/4/4
 */
@DsonLiteCodecScanIgnore
@DsonCodecScanIgnore
public class ObjectArrayCodec implements DuplexCodec<Object[]> {

    @Nonnull
    @Override
    public Class<Object[]> getEncoderClass() {
        return Object[].class;
    }

    @Override
    public void writeObject(DsonLiteObjectWriter writer, Object[] instance, TypeArgInfo<?> typeArgInfo) {
        TypeArgInfo<?> componentArgInfo = ConverterUtils.findComponentTypeArg(typeArgInfo.declaredType);
        for (Object e : instance) {
            writer.writeObject(0, e, componentArgInfo);
        }
    }

    @Override
    public Object[] readObject(DsonLiteObjectReader reader, TypeArgInfo<?> typeArgInfo) {
        ArrayList<Object> result = new ArrayList<>();
        TypeArgInfo<?> componentArgInfo = ConverterUtils.findComponentTypeArg(typeArgInfo.declaredType);
        while (reader.readDsonType() != DsonType.END_OF_OBJECT) {
            result.add(reader.readObject(0, componentArgInfo));
        }
        // 一定不是基础类型数组
        return (Object[]) ConverterUtils.convertList2Array(result, typeArgInfo.declaredType);
    }

    @Override
    public void writeObject(DsonObjectWriter writer, Object[] instance, TypeArgInfo<?> typeArgInfo, ObjectStyle style) {
        TypeArgInfo<?> componentArgInfo = ConverterUtils.findComponentTypeArg(typeArgInfo.declaredType);
        for (Object e : instance) {
            writer.writeObject(null, e, componentArgInfo, null);
        }
    }

    @Override
    public Object[] readObject(DsonObjectReader reader, TypeArgInfo<?> typeArgInfo) {
        ArrayList<Object> result = new ArrayList<>();
        TypeArgInfo<?> componentArgInfo = ConverterUtils.findComponentTypeArg(typeArgInfo.declaredType);
        while (reader.readDsonType() != DsonType.END_OF_OBJECT) {
            result.add(reader.readObject(null, componentArgInfo));
        }
        // 一定不是基础类型数组
        return (Object[]) ConverterUtils.convertList2Array(result, typeArgInfo.declaredType);
    }
}