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

import java.util.List;
import java.util.Objects;

/**
 * @author wjybxx
 * date - 2023/6/3
 */
public class DsonStringBuffer extends AbstractDsonBuffer<LineInfo> {

    private final CharSequence buffer;

    public DsonStringBuffer(CharSequence buffer) {
        this.buffer = Objects.requireNonNull(buffer);
    }

    @Override
    protected int charAt(LineInfo curLine, int position) {
        return buffer.charAt(position);
    }

    @Override
    protected void scanNextLine() {
        CharSequence buffer = this.buffer;
        List<LineInfo> lines = this.lines;
        int bufferLength = buffer.length();

        int startPos = indexNextLineStartPos(buffer, lines, bufferLength);
        int ln = getNextLn(lines);
        int endPos = startPos;
        while (endPos < bufferLength) {
            char c = buffer.charAt(endPos);
            boolean crlf = DsonTexts.isCRLF(c, buffer, endPos);
            if (crlf || endPos == bufferLength - 1) {
                if (startPos == endPos) { // 空行
                    startPos = endPos = endPos + DsonTexts.lengthCRLF(c);
                    continue;
                }
                if (endPos == bufferLength - 1 && !crlf) { // eof - parse需要扫描到该位置
                    endPos = bufferLength;
                }
                LheadType lheadType = parseLhead(buffer, startPos, endPos, ln);
                if (lheadType == LheadType.COMMENT) { // 注释行
                    startPos = endPos = endPos + DsonTexts.lengthCRLF(c);
                    continue;
                }
                int contentStartPos = indexContentStart(buffer, startPos, endPos);
                lines.add(new LineInfo(startPos, endPos, contentStartPos,
                        lheadType, ln, lines.size()));
                break;
            }
            endPos++;
        }
    }

    static int getNextLn(List<LineInfo> lines) {
        if (lines.size() == 0) return 1;
        return lines.get(lines.size() - 1).ln + 1;
    }

    static int indexNextLineStartPos(CharSequence buffer, List<LineInfo> lines, int bufferLength) {
        int startPos = 0;
        if (lines.size() > 0) {
            LineInfo lastLine = lines.get(lines.size() - 1);
            startPos = lastLine.endPos;
            // 先跳过当前行换行符，如果不是最后一行
            while (startPos < bufferLength && buffer.charAt(startPos) != '\n') {
                startPos++;
            }
            if (startPos < bufferLength) {
                startPos++;
            }
        }
        return startPos;
    }

    @Override
    public void close() {

    }
}