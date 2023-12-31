﻿#region LICENSE

//  Copyright 2023-2024 wjybxx(845740757@qq.com)
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

using System.Text;
using Wjybxx.Commons.IO;
using Wjybxx.Commons.Pool;

#pragma warning disable CS1591
namespace Wjybxx.Dson.Text;

/// <summary>
/// 文本解析器的设置
/// </summary>
public class DsonTextReaderSettings : DsonReaderSettings
{
    public static readonly DsonTextReaderSettings Default = NewBuilder().Build();

    /** StringBuilder池 - 用于Scanner扫描文本时 */
    public readonly IObjectPool<StringBuilder> StringBuilderPool;

    public DsonTextReaderSettings(Builder builder) : base(builder) {
        StringBuilderPool = builder.StringBuilderPool ?? LocalStringBuilderPool.Instance;
    }

    public new static Builder NewBuilder() {
        return new Builder();
    }

    public new class Builder : DsonReaderSettings.Builder
    {
        /** StringBuilder池 */
        public IObjectPool<StringBuilder>? StringBuilderPool = LocalStringBuilderPool.Instance;

        public Builder() {
        }

        public override DsonTextReaderSettings Build() {
            return new DsonTextReaderSettings(this);
        }
    }
}