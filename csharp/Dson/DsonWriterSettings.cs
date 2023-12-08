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

namespace Dson;

public class DsonWriterSettings
{
    public readonly int recursionLimit;
    public readonly bool autoClose;

    public DsonWriterSettings(Builder builder) {
        this.recursionLimit = Math.Max(1, builder._recursionLimit);
        this.autoClose = builder._autoClose;
    }

    public static Builder NewBuilder() {
        return new Builder();
    }

    public class Builder
    {
        /** 递归深度限制 */
        internal int _recursionLimit = 32;
        /** 是否自动关闭底层的输入输出流 */
        internal bool _autoClose = true;

        internal Builder() {
        }

        public virtual DsonWriterSettings Build() {
            return new DsonWriterSettings(this);
        }

        public int RecursionLimit {
            get => _recursionLimit;
            set => _recursionLimit = value;
        }
        public bool AutoClose {
            get => _autoClose;
            set => _autoClose = value;
        }
    }
}