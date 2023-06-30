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

import javax.annotation.Nonnull;

/**
 * @author wjybxx
 * date - 2023/4/19
 */
public abstract class DsonValue {

    @Nonnull
    public abstract DsonType getDsonType();

    public DsonInt32 asInt32() {
        return (DsonInt32) this;
    }

    public DsonInt64 asInt64() {
        return (DsonInt64) this;
    }

    public DsonFloat asFloat() {
        return (DsonFloat) this;
    }

    public DsonDouble asDouble() {
        return (DsonDouble) this;
    }

    public DsonBool asBoolean() {
        return (DsonBool) this;
    }

    public DsonString asString() {
        return (DsonString) this;
    }

    public DsonNull asNull() {
        return (DsonNull) this;
    }

    public DsonNumber asNumber() {
        return (DsonNumber) this;
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

    public DsonObjectRef asReference() {
        return (DsonObjectRef) this;
    }

    public DsonTimestamp asTimestamp() {
        return (DsonTimestamp) this;
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

}