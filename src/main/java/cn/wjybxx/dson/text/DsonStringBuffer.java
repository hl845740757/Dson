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

import cn.wjybxx.dson.internal.CollectionUtils;

import java.util.ArrayList;
import java.util.List;

/**
 * 基于完整的字符串解析
 *
 * @author wjybxx
 * date - 2023/6/5
 */
final class DsonStringBuffer implements DsonBuffer {

    private CharSequence buffer;
    private final boolean jsonLike;

    private final List<LineInfo> lines = new ArrayList<>();
    private LineInfo curLine;
    private boolean startLine = false;
    private int position = -1;
    private boolean eof = false;

    DsonStringBuffer(CharSequence buffer, boolean jsonLike) {
        this.buffer = buffer;
        this.jsonLike = jsonLike;
    }

    private void setCurLine(LineInfo curLine) {
        this.curLine = curLine;
    }

    @Override
    public int readSlowly() {
        if (buffer == null) throw new DsonParseException("Trying to read after closed");
        if (eof) throw new DsonParseException("Trying to read past eof");

        // 首次读
        if (position == -1) {
            return onFirstRead();
        }
        LineInfo curLine = this.curLine;
        if (!curLine.hasContent()) { // 无可读内容
            position = curLine.endPos;
            return onReadEndOfLine();
        } else if (!startLine) { // 初始读
            startLine = true;
        } else if (position + 1 >= curLine.endPos) { // 读完或已在行尾(unread)
            position = curLine.endPos;
            return onReadEndOfLine();
        } else {
            position++;
        }
        return buffer.charAt(position);
    }

    private int onFirstRead() {
        position = 0;
        if (lines.isEmpty() && !scanNextLine()) {
            eof = true;
            return -1;
        } else {
            LineInfo curLine = lines.get(0);
            onReadNextLine(curLine);
            return -2;
        }
    }

    private int onReadEndOfLine() {
        List<LineInfo> lines = this.lines;
        LineInfo curLine = this.curLine;
        // 由于存在unread，因此要检查，避免过早解析
        int index = indexCurLine(lines, curLine);
        if (index + 1 == lines.size() && !scanNextLine()) {
            eof = true;
            return -1;
        } else {
            curLine = lines.get(index + 1);
            onReadNextLine(curLine);
            return -2;
        }
    }

    private void onReadNextLine(LineInfo curLine) {
        setCurLine(curLine);
        position = curLine.contentStartPos;
        startLine = false;
    }

    @Override
    public void unread() {
        if (position == -1) {
            throw new IllegalStateException("read must be called before unread.");
        }
        if (eof) {
            eof = false;
            return;
        }
        // 有内容时尝试回退单个字符
        LineInfo curLine = this.curLine;
        if (startLine) {
            if (position > curLine.contentStartPos) {
                position--;
            } else {
                startLine = false;
            }
            return;
        }
        // 尝试回退到上一行
        int index = indexCurLine(lines, curLine);
        if (index > 0) {
            // 回退到上一行
            curLine = lines.get(index - 1);
            setCurLine(curLine);
            position = curLine.endPos;
            startLine = curLine.hasContent();
        } else {
            // 回退到初始状态
            setCurLine(null);
            position = -1;
            startLine = false;
        }
    }

    @Override
    public LheadType lhead() {
        if (curLine == null) throw new IllegalStateException("read must be called before lhead");
        return curLine.lheadType;
    }

    @Override
    public int getPosition() {
        return position;
    }

    @Override
    public int getLn() {
        return curLine == null ? 0 : curLine.ln;
    }

    @Override
    public int getCol() {
        return curLine == null ? 0 : (position - curLine.startPos + 1);
    }

    @Override
    public void close() {
        buffer = null;
        lines.clear();
    }

    //
    private boolean scanNextLine() {
        CharSequence buffer = this.buffer;
        List<LineInfo> lines = this.lines;
        int bufferLength = buffer.length();

        int startPos;
        int ln;
        if (lines.size() > 0) {
            LineInfo lastLine = lines.get(lines.size() - 1);
            if (lastLine.endPos >= bufferLength) {
                return false;
            }
            startPos = lastLine.endPos + DsonTexts.lengthCRLF(buffer.charAt(lastLine.endPos));
            ln = lastLine.ln + 1;
        } else {
            startPos = 0;
            ln = 1;
        }
        int endPos = startPos;
        while (endPos < bufferLength) {
            char c = buffer.charAt(endPos);
            boolean crlf = DsonTexts.isCRLF(c, buffer, endPos);
            if (crlf || endPos == bufferLength - 1) {
                if (startPos == endPos) { // 空行
                    startPos = endPos = endPos + DsonTexts.lengthCRLF(c);
                    ln++;
                    continue;
                }
                if (!crlf) { // eof - parse需要扫描到该位置
                    endPos = bufferLength;
                }

                LheadType lheadType;
                int contentStartPos;
                if (jsonLike) {
                    lheadType = LheadType.APPEND;
                    contentStartPos = startPos;
                } else {
                    lheadType = DsonTexts.parseLhead(buffer, startPos, endPos);
                    if (lheadType == LheadType.COMMENT) { // 注释行
                        startPos = endPos = endPos + DsonTexts.lengthCRLF(c);
                        ln++;
                        continue;
                    }
                    contentStartPos = DsonTexts.indexContentStart(buffer, startPos, endPos);
                }
                lines.add(new LineInfo(ln, startPos, endPos, lheadType, contentStartPos));
                return true;
            }
            endPos++;
        }
        return false;
    }

    static int indexCurLine(List<? extends LineInfo> lines, LineInfo curLine) {
        return CollectionUtils.lastIndexOfRef(lines, curLine);
    }

}