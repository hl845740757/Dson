#region LICENSE

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

using System;
using System.IO;
using System.Text;
using Wjybxx.Commons.IO;
using Wjybxx.Commons.Pool;

#pragma warning disable CS1591
namespace Wjybxx.Dson.Text;

/// <summary>
/// DsonTextWriter的设置
/// </summary>
public class DsonTextWriterSettings : DsonWriterSettings
{
    private const int MinLineLength = 10;
    /** 标准模式默认设置 */
    public static readonly DsonTextWriterSettings Default;
    /** 宽松模式默认设置 */
    public static readonly DsonTextWriterSettings RelaxedDefault;

    /** 行分隔符 */
    public readonly string LineSeparator;
    /** Dson文本模式 */
    public readonly DsonMode DsonMode;
    /** 行长度-软限制 */
    public readonly int SoftLineLength;

    /** StringBuilder池 */
    public readonly IObjectPool<StringBuilder> StringBuilderPool;
    /** 目标Writer是<see cref="StringWriter"/>时是否直接使用底层的builder */
    public readonly bool AccessBackingBuilder;

    /** 是否启用文本模式输出 */
    public readonly bool EnableText;
    /** 触发文本输出的字符串长度 */
    public readonly float TextStringLength;
    /** 文本字符串换行是否启用左对齐 */
    public readonly bool TextAlignLeft;
    /** 双引号字符串换行是否启用左对齐 */
    public readonly bool StringAlignLeft;

    /** 不可打印的ascii码字符是否转为unicode字符 */
    public readonly bool UnicodeChar;
    /** 无引号字符串的最大长度 */
    public readonly int MaxLengthOfUnquoteString;
    /** 行首前的额外缩进 */
    public readonly int ExtraIndent;

    public DsonTextWriterSettings(Builder builder) : base(builder) {
        this.LineSeparator = builder.LineSeparator;
        this.DsonMode = builder.DsonMode;
        this.SoftLineLength = Math.Max(MinLineLength, builder.SoftLineLength);
        this.StringBuilderPool = builder.StringBuilderPool ?? LocalStringBuilderPool.Instance;
        this.AccessBackingBuilder = builder.AccessBackingBuilder;

        // 标准模式下才可启用纯文本
        this.EnableText = DsonMode == DsonMode.Standard && builder.EnableText;
        this.TextStringLength = Math.Max(MinLineLength, builder.TextStringLength);
        this.TextAlignLeft = builder.TextAlignLeft;
        this.StringAlignLeft = builder.StringAlignLeft;

        this.UnicodeChar = builder.UnicodeChar;
        this.MaxLengthOfUnquoteString = builder.MaxLengthOfUnquoteString;
        this.ExtraIndent = Math.Max(0, builder.ExtraIndent);
    }

    static DsonTextWriterSettings() {
        Default = NewBuilder().Build();

        Builder relaxedBuilder = NewBuilder();
        relaxedBuilder.DsonMode = DsonMode.Relaxed;
        RelaxedDefault = relaxedBuilder.Build();
    }

    public new static Builder NewBuilder() {
        return new Builder();
    }

    public new class Builder : DsonWriterSettings.Builder
    {
        /** 行分隔符 */
        public string LineSeparator = Environment.NewLine;
        /** 文本模式 */
        public DsonMode DsonMode = DsonMode.Standard;
        /**
         * 行长度，该值是一个换行参考值
         * 精确控制行长度较为复杂，那样我们需要考虑每一种值toString后长度超出的问题；
         * 另外在美观性上也不好，比如：一个integer写到一半换行。。。
         * 另外，这个行长度是是码元计数，不是字符计数。
         */
        public int SoftLineLength = 120;

        /** StringBuilder池 */
        public IObjectPool<StringBuilder>? StringBuilderPool = LocalStringBuilderPool.Instance;
        /** 如果目标Writer是<see cref="StringWriter"/>，是否直接访问底层的Builder代替额外的分配，这可以节省大量的开销。 */
        public bool AccessBackingBuilder = true;

        /**
         * 是否启用纯文本模式
         * 如果{@link #unicodeChar}为true，该值通常需要关闭，text模式不会执行转义，也就不会处理unicode字符
         */
        public bool EnableText = true;
        /** 触发text模式的字符串长度 */
        public float TextStringLength = 120;
        /** 纯文本换行是否启用左对齐 -- 仅标准模式下生效 */
        public bool TextAlignLeft = false;
        /** 字符串换行是否启用左对齐 -- 仅标准模式下生效；不适用无引号字符串 */
        public bool StringAlignLeft = false;

        /**
         * 不可打印的ascii码字符是否转为unicode字符
         * (ascii码32~126以外的字符)
         * 通常用于非UTF8文本的移植
         */
        public bool UnicodeChar = false;
        /** 自动模式下无引号字符串的最大长度 -- 过大会降低序列化速度 */
        public int MaxLengthOfUnquoteString = 20;
        /** 外层额外缩进 */
        public int ExtraIndent;

        public Builder() {
        }

        public override DsonTextWriterSettings Build() {
            return new DsonTextWriterSettings(this);
        }
    }
}