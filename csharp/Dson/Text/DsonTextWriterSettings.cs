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
namespace Dson.Text;

public class DsonTextWriterSettings : DsonWriterSettings
{
    public readonly String lineSeparator;
    public readonly int softLineLength;
    public readonly DsonMode dsonMode;

    public readonly bool enableText;
    public readonly float lengthFactorOfText;
    public readonly bool unicodeChar;
    public readonly int maxLengthOfUnquoteString;

    public DsonTextWriterSettings(Builder builder) : base(builder) {
        this.lineSeparator = builder.lineSeparator;
        this.softLineLength = Math.Max(8, builder.softLineLength);
        this.dsonMode = builder.dsonMode;

        // 标准模式下才可启用纯文本
        this.enableText = dsonMode == DsonMode.STANDARD && builder.enableText;
        this.lengthFactorOfText = builder.lengthFactorOfText;
        this.unicodeChar = builder.unicodeChar;
        this.maxLengthOfUnquoteString = builder.maxLengthOfUnquoteString;
    }

    public new static Builder NewBuilder() {
        return new Builder();
    }

    public new class Builder : DsonWriterSettings.Builder
    {
        /** 行分隔符 */
        internal string lineSeparator = Environment.NewLine;
        /**
         * 行长度，该值是一个换行参考值
         * 精确控制行长度较为复杂，那样我们需要考虑每一种值toString后长度超出的问题；
         * 另外在美观性上也不好，比如：一个integer写到一半换行。。。
         * 另外，这个行长度是是码元计数，不是字符计数。
         */
        internal int softLineLength = 120;
        /**
         * 文本模式
         */
        internal DsonMode dsonMode = DsonMode.STANDARD;
        /**
         * 是否支持纯文本模式
         * 如果{@link #unicodeChar}为true，该值通常需要关闭，text模式不会执行转义，也就不会处理unicode字符
         */
        internal bool enableText = true;
        /**
         * 当字符串的长度达到 lineLength  * factor 触发text模式
         */
        internal float lengthFactorOfText = 2;
        /**
         * 不可打印的ascii码字符是否转为unicode字符
         * (ascii码32~126以外的字符)
         * 通常用于非UTF8文本的移植
         */
        internal bool unicodeChar = false;
        /**
         * 自动模式下无引号字符串的最大长度
         */
        internal int maxLengthOfUnquoteString = 66;

        internal Builder() {
        }

        public string LineSeparator {
            get => lineSeparator;
            set => lineSeparator = value ?? throw new ArgumentNullException(nameof(value));
        }
        public int SoftLineLength {
            get => softLineLength;
            set => softLineLength = value;
        }
        public DsonMode DsonMode {
            get => dsonMode;
            set => dsonMode = value;
        }
        public bool EnableText {
            get => enableText;
            set => enableText = value;
        }
        public float LengthFactorOfText {
            get => lengthFactorOfText;
            set => lengthFactorOfText = value;
        }
        public bool UnicodeChar {
            get => unicodeChar;
            set => unicodeChar = value;
        }
        public int MaxLengthOfUnquoteString {
            get => maxLengthOfUnquoteString;
            set => maxLengthOfUnquoteString = value;
        }
        public int RecursionLimit1 {
            get => _recursionLimit;
            set => _recursionLimit = value;
        }
        public bool AutoClose1 {
            get => _autoClose;
            set => _autoClose = value;
        }
    }
}