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

import java.util.Objects;

/**
 * 基于完整的字符串解析
 *
 * @author wjybxx
 * date - 2023/6/5
 */
final class StringCharStream extends AbstractCharStream {

    private CharSequence buffer;

    public StringCharStream(CharSequence buffer) {
        this.buffer = Objects.requireNonNull(buffer);
        int idx = DsonTexts.indexOfNonWhitespace(buffer, 0);
        if (idx < 0) {
            dsonMode = DsonMode.RELAXED;
            setPosition(buffer.length());
        } else {
            dsonMode = DsonTexts.detectDsonMode(buffer.charAt(idx));
            setPosition(idx - 1);
        }
    }

    @Override
    public void close() {
        buffer = null;
    }

    @Override
    protected boolean isClosed() {
        return buffer == null;
    }

    @Override
    protected int charAt(LineInfo curLine, int position) {
        return buffer.charAt(position);
    }

    @Override
    protected void checkUnreadOverFlow(int position) {
        if (position < 0 || position >= buffer.length()) {
            throw bufferOverFlow(position);
        }
    }

    @Override
    protected void scanMoreChars(LineInfo line) {

    }

    @Override
    protected boolean scanNextLine() {
        CharSequence buffer = this.buffer;
        int bufferLength = buffer.length();

        LineInfo curLine = getCurLine();
        final int startPos;
        final int ln;
        if (curLine == null) {
            ln = getFirstLn();
            startPos = 0;
        } else {
            ln = curLine.ln + 1;
            startPos = curLine.endPos + 1;
        }
        if (startPos >= bufferLength) {
            return false;
        }

        int state = LineInfo.STATE_SCAN;
        int endPos = startPos;
        int headPos = -1;

        for (; endPos < bufferLength; endPos++) {
            char c = buffer.charAt(endPos);
            // 需要放在switch-case之前，否则可能漏掉\r的非法head
            if (headPos == -1 && !DsonTexts.isIndentChar(c)) {
                headPos = endPos;
            }
            if (c == '\n') {
                state = LineInfo.STATE_LF;
                break;
            }
            if (c == '\r') {
                if (endPos == bufferLength - 1) { // eof
                    state = LineInfo.STATE_EOF;
                    break;
                }
                c = buffer.charAt(++endPos);
                if (c == '\n') { // CRLF
                    state = LineInfo.STATE_CRLF;
                    break;
                }
            }
            if (endPos == bufferLength - 1) { // eof
                state = LineInfo.STATE_EOF;
                break;
            }
        }

        LineHead lineHead = LineHead.COMMENT;
        int contentStartPos = -1;
        int lastReadablePos = LineInfo.lastReadablePosition(state, endPos);
        if (dsonMode == DsonMode.RELAXED) {
            if (startPos <= lastReadablePos) {
                lineHead = LineHead.APPEND;
                contentStartPos = startPos;
            }
        } else {
            if (headPos >= startPos && headPos <= lastReadablePos) {
                String label = Character.toString(buffer.charAt(headPos));
                lineHead = LineHead.forLabel(label);
                if (lineHead == null) {
                    throw new DsonParseException("Unknown head %s, pos: %d".formatted(label, headPos));
                }
                // 检查缩进
                if (headPos + 1 <= lastReadablePos && buffer.charAt(headPos + 1) != ' ') {
                    throw new DsonParseException("space is required, head %s, pos: %d".formatted(label, headPos));
                }
                // 确定内容开始位置
                if (headPos + 2 <= lastReadablePos) {
                    contentStartPos = headPos + 2;
                }
            }
        }
        LineInfo tempLine = new LineInfo(ln, startPos, endPos, lineHead, contentStartPos);
        tempLine.state = state;
        addLine(tempLine);
        return true;
    }

}