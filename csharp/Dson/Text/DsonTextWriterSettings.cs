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

namespace Dson.Text;

public class DsonTextWriterSettings : DsonWriterSettings
{
    public readonly string LineSeparator;
    public readonly int SoftLineLength;
    public readonly DsonMode DsonMode;

    public readonly bool EnableText;
    public readonly float LengthFactorOfText;
    public readonly bool UnicodeChar;
    public readonly int MaxLengthOfUnquoteString;

    public DsonTextWriterSettings(Builder builder) : base(builder) {
        this.LineSeparator = builder.LineSeparator;
        this.SoftLineLength = Math.Max(8, builder.SoftLineLength);
        this.DsonMode = builder.DsonMode;

        // 标准模式下才可启用纯文本
        this.EnableText = DsonMode == DsonMode.Standard && builder.EnableText;
        this.LengthFactorOfText = builder.LengthFactorOfText;
        this.UnicodeChar = builder.UnicodeChar;
        this.MaxLengthOfUnquoteString = builder.MaxLengthOfUnquoteString;
    }

    public static readonly DsonTextWriterSettings Default;
    public static readonly DsonTextWriterSettings RelaxedDefault;

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
        /**
         * 行长度，该值是一个换行参考值
         * 精确控制行长度较为复杂，那样我们需要考虑每一种值toString后长度超出的问题；
         * 另外在美观性上也不好，比如：一个integer写到一半换行。。。
         * 另外，这个行长度是是码元计数，不是字符计数。
         */
        public int SoftLineLength = 120;
        /**
         * 文本模式
         */
        public DsonMode DsonMode = DsonMode.Standard;
        /**
         * 是否支持纯文本模式
         * 如果{@link #unicodeChar}为true，该值通常需要关闭，text模式不会执行转义，也就不会处理unicode字符
         */
        public bool EnableText = true;
        /**
         * 当字符串的长度达到 lineLength  * factor 触发text模式
         */
        public float LengthFactorOfText = 2;
        /**
         * 不可打印的ascii码字符是否转为unicode字符
         * (ascii码32~126以外的字符)
         * 通常用于非UTF8文本的移植
         */
        public bool UnicodeChar = false;
        /**
         * 自动模式下无引号字符串的最大长度
         */
        public int MaxLengthOfUnquoteString = 66;

        public Builder() {
        }

        public override DsonTextWriterSettings Build() {
            return new DsonTextWriterSettings(this);
        }
    }
}