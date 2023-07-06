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

/**
 * 换行一定的行信息可以有效避免unread时反向扫描整行
 *
 * @author wjybxx
 * date - 2023/6/3
 */
class LineInfo {

    /** 行号 */
    final int ln;
    /** 行全局起始位置，包含行首 */
    final int startPos;
    /**
     * 行结束位置（全局） -- 有效输入的结束。
     * 1.如果换行符是\r\n，则是\r的位置；
     * 2.如果换行符是\n，则是\n的位置；
     * 3.eof的情况下，是最后一个字符+1；即：内容始终不包含该位置。
     * 4.start和end相等时表示空行；
     */
    int endPos;

    /** 行首类型 */
    final LheadType lheadType;
    /** 内容全局起始位置 */
    final int contentStartPos;

    public LineInfo(int ln, int startPos, int endPos, LheadType lheadType, int contentStartPos) {
        this.ln = ln;
        this.startPos = startPos;
        this.endPos = endPos;
        this.lheadType = lheadType;
        this.contentStartPos = contentStartPos;
    }

    public boolean hasContent() {
        return contentStartPos < endPos;
    }

    @Override
    public String toString() {
        return "LineInfo{" +
                " startPos=" + startPos +
                ", endPos=" + endPos +
                ", contentStartPos=" + contentStartPos +
                ", lheadType=" + lheadType +
                ", ln=" + ln +
                '}';
    }
}