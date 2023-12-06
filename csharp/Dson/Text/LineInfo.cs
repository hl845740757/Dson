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

using System.Text;

namespace Dson.Text;

public class LineInfo
{
    public const int STATE_SCAN = 0;
    public const int STATE_LF = 1;
    public const int STATE_CRLF = 2;
    public const int STATE_EOF = 3;

    /** 行号 */
    public readonly int ln;

    /** 行全局起始位置， 0-based */
    public readonly int startPos;
    /**
     * 行结束位置（全局），0-based
     * 1.如果换行符是\r\n，则是\n的位置；
     * 2.如果换行符是\n，则是\n的位置；
     * 3.eof的情况下，是最后一个字符的位置 --换行结束的情况下，eof出现在读取下一行的时候
     * 4.start和end相等时表示空行；
     */
    public int endPos;
    /** 行在字符流中的状态 -- endPos是否到达行尾 */
    public int state = STATE_SCAN;

    /** 行首类型 */
    public readonly LheadType lheadType;
    /**
     * 内容全局起始位置 -- -1表示无内容
     * 由于文件可能是没有行首的，因此不能记录行首的开始位置;
     */
    public readonly int contentStartPos;

    public LineInfo(int ln, int startPos, int endPos, LheadType lheadType, int contentStartPos) {
        this.ln = ln;
        this.startPos = startPos;
        this.endPos = endPos;
        this.lheadType = lheadType;
        this.contentStartPos = contentStartPos;
    }

    /** 当前行是否已扫描完成 */
    public bool isScanCompleted() {
        return state != STATE_SCAN;
    }

    /** 最后一个可读取的位置 */
    public int lastReadablePosition() {
        return lastReadablePosition(state, endPos);
    }

    /** 最后一个可读取的位置 -- 不包含换行符；可能小于startPos */
    public static int lastReadablePosition(int state, int endPos) {
        if (state == STATE_LF) {
            return endPos - 1;
        }
        if (state == STATE_CRLF) {
            return endPos - 2;
        }
        return endPos;
    }

    public bool hasContent() {
        return contentStartPos != -1;
    }

    public int lineLength() {
        if (endPos < startPos) {
            return 0;
        }
        return endPos - startPos + 1;
    }

    public LheadType getLheadType() {
        return lheadType;
    }

    public int getContentStartPos() {
        return contentStartPos;
    }

    public override bool Equals(Object o) {
        return this == o;
    }

    public override int GetHashCode() {
        return ln;
    }

    public override string ToString() {
        return new StringBuilder(64)
            .Append("LineInfo{")
            .Append("ln=").Append(ln)
            .Append(", startPos=").Append(startPos)
            .Append(", endPos=").Append(endPos)
            .Append(", state=").Append(state)
            .Append(", lheadType=").Append(lheadType)
            .Append(", contentStartPos=").Append(contentStartPos)
            .Append('}').ToString();
    }
}