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

package cn.wjybxx.dson;

import cn.wjybxx.dson.types.ObjectRef;
import cn.wjybxx.dson.types.OffsetTimestamp;

import javax.annotation.Nonnull;

/**
 * @author wjybxx
 * date - 2023/4/19
 */
public abstract class DsonValue {

    @Nonnull
    public abstract DsonType getDsonType();

    // region 拆箱类型
    public int asInt32() {
        return ((DsonInt32) this).intValue();
    }

    public long asInt64() {
        return ((DsonInt64) this).longValue();
    }

    public float asFloat() {
        return ((DsonFloat) this).floatValue();
    }

    public double asDouble() {
        return ((DsonDouble) this).doubleValue();
    }

    public boolean asBool() {
        return ((DsonBool) this).getValue();
    }

    public String asString() {
        return ((DsonString) this).getValue();
    }

    /** {@link Number}不是接口导致需要额外的装箱 */
    public Number asNumber() {
        return ((DsonNumber) this).number();
    }

    public ObjectRef asReference() {
        return ((DsonReference) this).getValue();
    }

    public OffsetTimestamp asTimestamp() {
        return ((DsonTimestamp) this).getValue();
    }

    // endregion

    // region 装箱类型

    public DsonInt32 asDsonInt32() {
        return (DsonInt32) this;
    }

    public DsonInt64 asDsonInt64() {
        return (DsonInt64) this;
    }

    public DsonFloat asDsonFloat() {
        return (DsonFloat) this;
    }

    public DsonDouble asDsonDouble() {
        return (DsonDouble) this;
    }

    public DsonBool asDsonBool() {
        return (DsonBool) this;
    }

    public DsonString asDsonString() {
        return (DsonString) this;
    }

    public DsonNumber asDsonNumber() {
        return (DsonNumber) this;
    }

    public DsonReference asDsonReference() {
        return (DsonReference) this;
    }

    public DsonTimestamp asDsonTimestamp() {
        return (DsonTimestamp) this;
    }

    // endregion

    // region Dson特定类型

    public DsonNull asNull() {
        return (DsonNull) this;
    }

    public DsonExtString asExtString() {
        return (DsonExtString) this;
    }

    public DsonExtInt32 asExtInt32() {
        return (DsonExtInt32) this;
    }

    public DsonExtInt64 asExtInt64() {
        return (DsonExtInt64) this;
    }

    public DsonBinary asBinary() {
        return (DsonBinary) this;
    }

    @SuppressWarnings("unchecked")
    public DsonHeader<String> asHeader() {
        return (DsonHeader<String>) this;
    }

    @SuppressWarnings("unchecked")
    public DsonArray<String> asArray() {
        return (DsonArray<String>) this;
    }

    @SuppressWarnings("unchecked")
    public DsonObject<String> asObject() {
        return (DsonObject<String>) this;
    }
    //

    @SuppressWarnings("unchecked")
    public DsonHeader<FieldNumber> asHeaderLite() {
        return (DsonHeader<FieldNumber>) this;
    }

    @SuppressWarnings("unchecked")
    public DsonArray<FieldNumber> asArrayLite() {
        return (DsonArray<FieldNumber>) this;
    }

    @SuppressWarnings("unchecked")
    public DsonObject<FieldNumber> asObjectLite() {
        return (DsonObject<FieldNumber>) this;
    }

    // endregion

}