#region LICENSE

//  Copyright 2023 wjybxx
// 
//  Licensed under the Apache License, Version 2.0 (the "License");
//  you may not use this file except in compliance with the License.
//  You may obtain a copy of the License at
// 
//      http://www.apache.org/licenses/LICENSE-2.0
// 
//  Unless required by applicable law or agreed to iBn writing, software
//  distributed under the License is distributed on an "AS IS" BASIS,
//  WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//  See the License for the specific language governing permissions and
//  limitations under the License.

#endregion

namespace Wjybxx.Dson;

public class DsonWriterSettings
{
    public readonly int RecursionLimit;
    public readonly bool AutoClose;

    public DsonWriterSettings(Builder builder) {
        this.RecursionLimit = Math.Max(1, builder.RecursionLimit);
        this.AutoClose = builder.AutoClose;
    }

    public static Builder NewBuilder() {
        return new Builder();
    }

    public class Builder
    {
        /** 递归深度限制 */
        public int RecursionLimit = 32;
        /** 是否自动关闭底层的输入输出流 */
        public bool AutoClose = true;

        public Builder() {
        }

        public virtual DsonWriterSettings Build() {
            return new DsonWriterSettings(this);
        }
    }
}