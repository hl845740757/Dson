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

package cn.wjybxx.dson.text;

import cn.wjybxx.base.ObjectUtils;
import cn.wjybxx.base.io.LocalStringBuilderPool;
import cn.wjybxx.base.pool.ObjectPool;
import cn.wjybxx.dson.DsonReaderSettings;

/**
 * @author wjybxx
 * date - 2023/10/14
 */
public class DsonTextReaderSettings extends DsonReaderSettings {

    public static final DsonTextReaderSettings DEFAULT = newBuilder().build();

    /** StringBuilder池 */
    public final ObjectPool<StringBuilder> stringBuilderPool;

    protected DsonTextReaderSettings(Builder builder) {
        super(builder);
        stringBuilderPool = ObjectUtils.nullToDef(builder.stringBuilderPool, LocalStringBuilderPool.INSTANCE);
    }

    public static Builder newBuilder() {
        return new Builder();
    }

    public static class Builder extends DsonReaderSettings.Builder {
        /** StringBuilder池 */
        private ObjectPool<StringBuilder> stringBuilderPool = LocalStringBuilderPool.INSTANCE;

        protected Builder() {
        }

        public ObjectPool<StringBuilder> getStringBuilderPool() {
            return stringBuilderPool;
        }

        public Builder setStringBuilderPool(ObjectPool<StringBuilder> stringBuilderPool) {
            this.stringBuilderPool = stringBuilderPool;
            return this;
        }

        @Override
        public DsonTextReaderSettings build() {
            return new DsonTextReaderSettings(this);
        }
    }
}