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

package cn.wjybxx.dson.text;

import javax.annotation.concurrent.Immutable;
import java.util.Objects;

/**
 * @author wjybxx
 * date - 2023/6/5
 */
@Immutable
public class DsonTextWriterSettings {

    public final String lineSeparator;
    public final int softLineLength;
    public final boolean enableText;
    public final float lengthFactorOfText;
    public final boolean unicodeChar;
    public final boolean disableSci;

    private DsonTextWriterSettings(Builder builder) {
        this.lineSeparator = Objects.requireNonNull(builder.lineSeparator);
        this.softLineLength = Math.max(8, builder.softLineLength);
        this.unicodeChar = builder.unicodeChar;
        this.enableText = builder.enableText;
        this.lengthFactorOfText = builder.lengthFactorOfText;
        this.disableSci = builder.disableSci;
    }

    public static Builder newBuilder() {
        return new Builder();
    }

    public static class Builder {

        /** 行分隔符 */
        private String lineSeparator = System.lineSeparator();
        /**
         * 行长度，该值是一个换行参考值
         * 精确控制行长度较为复杂，那样我们需要考虑每一种值toString后长度超出的问题；
         * 另外在美观性上也不好，比如：一个integer写到一半换行。。。
         * 另外，这个行长度是是码元计数，不是字符计数。
         */
        private int softLineLength = 120;
        /**
         * 是否支持纯文本模式
         * 如果{@link #unicodeChar}为true，该值通常需要关闭，text模式不会执行转义，也就不会处理unicode字符
         */
        private boolean enableText = true;
        /**
         * 当字符串的长度达到 lineLength  * factor 触发text模式
         */
        private float lengthFactorOfText = 2;
        /**
         * 不可打印的ascii码字符是否转为unicode字符
         * (ascii码32~126以外的字符)
         * 通常用于非UTF8文本的移植
         */
        private boolean unicodeChar = false;
        /**
         * 是否禁用科学计数法输出
         */
        private boolean disableSci = false;

        private Builder() {
        }

        public DsonTextWriterSettings build() {
            return new DsonTextWriterSettings(this);
        }

        public String getLineSeparator() {
            return lineSeparator;
        }

        public Builder setLineSeparator(String lineSeparator) {
            this.lineSeparator = lineSeparator;
            return this;
        }

        public int getSoftLineLength() {
            return softLineLength;
        }

        public Builder setSoftLineLength(int softLineLength) {
            this.softLineLength = softLineLength;
            return this;
        }

        public boolean isUnicodeChar() {
            return unicodeChar;
        }

        public Builder setUnicodeChar(boolean unicodeChar) {
            this.unicodeChar = unicodeChar;
            return this;
        }

        public boolean isEnableText() {
            return enableText;
        }

        public Builder setEnableText(boolean enableText) {
            this.enableText = enableText;
            return this;
        }

        public float getLengthFactorOfText() {
            return lengthFactorOfText;
        }

        public Builder setLengthFactorOfText(float lengthFactorOfText) {
            this.lengthFactorOfText = lengthFactorOfText;
            return this;
        }

        public boolean isDisableSci() {
            return disableSci;
        }

        public Builder setDisableSci(boolean disableSci) {
            this.disableSci = disableSci;
            return this;
        }
    }

}