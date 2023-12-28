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

import cn.wjybxx.dson.types.*;

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

    public Binary asBinary() {
        return ((DsonBinary) this).binary();
    }

    public ExtInt32 asExtInt32() {
        return ((DsonExtInt32) this).extInt32();
    }

    public ExtInt64 asExtInt64() {
        return ((DsonExtInt64) this).extInt64();
    }

    public ExtDouble asExtDouble() {
        return ((DsonExtDouble) this).extDouble();
    }

    public ExtString asExtString() {
        return ((DsonExtString) this).extString();
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

    public DsonBinary asDsonBinary() {
        return (DsonBinary) this;
    }

    public DsonExtInt32 asDsonExtInt32() {
        return (DsonExtInt32) this;
    }

    public DsonExtInt64 asDsonExtInt64() {
        return (DsonExtInt64) this;
    }

    public DsonExtDouble asDsonExtDouble() {
        return (DsonExtDouble) this;
    }

    public DsonExtString asDsonExtString() {
        return (DsonExtString) this;
    }

    public DsonReference asDsonReference() {
        return (DsonReference) this;
    }

    public DsonTimestamp asDsonTimestamp() {
        return (DsonTimestamp) this;
    }

    public DsonNull asDsonNull() {
        return (DsonNull) this;
    }

    public DsonNumber asDsonNumber() {
        return (DsonNumber) this;
    }
    // endregion

    // region Dson特定类型

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