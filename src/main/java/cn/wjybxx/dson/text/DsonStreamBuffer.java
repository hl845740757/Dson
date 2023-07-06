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

import cn.wjybxx.dson.internal.InternalUtils;
import org.apache.commons.lang3.exception.ExceptionUtils;

import javax.annotation.Nonnull;
import javax.annotation.Nullable;
import java.io.IOException;
import java.io.Reader;
import java.util.ArrayList;
import java.util.List;

/**
 * @author wjybxx
 * date - 2023/6/5
 */
final class DsonStreamBuffer implements DsonBuffer {

    private static final int MIN_BUFFER_SIZE = 16;

    private Reader reader;
    private final boolean jsonLike;
    private final int bufferSizePerLine;

    private final List<StreamLineInfo> lines = new ArrayList<>();
    private StreamLineInfo curLine;
    private boolean startLine = false;
    private int position = -1;
    private boolean eof = false;

    /** 首个有效行，用于回退判定 */
    private StreamLineInfo firstLine;
    /** reader是否到已到达文件尾 */
    private boolean readerEof;

    DsonStreamBuffer(Reader reader, boolean jsonLike) {
        this(reader, jsonLike, MIN_BUFFER_SIZE * 2);
    }

    DsonStreamBuffer(Reader reader, boolean jsonLike, int bufferSizePerLine) {
        this.reader = reader;
        this.jsonLike = jsonLike;
        this.bufferSizePerLine = Math.max(MIN_BUFFER_SIZE, bufferSizePerLine);
    }

    private void setCurLine(StreamLineInfo curLine) {
        this.curLine = curLine;
    }

    @Override
    public int readSlowly() {
        if (reader == null) throw new DsonParseException("Trying to read after closed");
        if (eof) throw new DsonParseException("Trying to read past eof");

        // 首次读
        if (position == -1) {
            return onFirstRead();
        }
        StreamLineInfo curLine = this.curLine;
        if (position == curLine.endPos && !curLine.isScanCompleted()) {
            curLine.discardReadChars(position);
            scanMoreChars(curLine); // 要么读取到一个输入，要么行扫描完毕；endPos至少加1
        }
        if (curLine.isScanCompleted()) {
            if (!curLine.hasContent()) { // 无可读内容
                position = curLine.endPos;
                return onReadEndOfLine();
            }
            if (!startLine) {
                startLine = true;
            } else if (position + 1 >= curLine.endPos) { // 读完或已在行尾(unread)
                position = curLine.endPos;
                return onReadEndOfLine();
            } else {
                position++;
            }
        } else {
//            assert position < curLine.endPos;
            if (!startLine) {
                startLine = true;
            } else {
                position++;
            }
        }
        return curLine.charAtPosition(position);
    }

    private int onFirstRead() {
        position = 0;
        // 由于存在unread，因此要检查，避免重复解析
        if (lines.isEmpty() && !scanNextLine()) {
            eof = true;
            return -1;
        } else {
            StreamLineInfo curLine = lines.get(0);
            onReadNextLine(curLine);
            firstLine = curLine;
            return -2;
        }
    }

    private int onReadEndOfLine() {
        List<StreamLineInfo> lines = this.lines;
        StreamLineInfo curLine = this.curLine;
        // 由于存在unread，因此要检查，避免过早解析
        int index = DsonStringBuffer.indexCurLine(lines, curLine);
        if (index + 1 == lines.size() && !scanNextLine()) {
            eof = true;
            return -1;
        } else {
            curLine = lines.get(index + 1);
            onReadNextLine(curLine);
            discardUnnecessaryLines();
            return -2;
        }
    }

    private void discardUnnecessaryLines() {
        // 考虑大文件的情况下，减少不必要的内存占用
        if (lines.size() > 5) {
            lines.subList(0, 2).clear();
            firstLine = null;
        }
    }

    private void onReadNextLine(StreamLineInfo curLine) {
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
        // 有内容时尝试回退单个字符 -- 需要检测是否回退到bufferStartPos之前
        StreamLineInfo curLine = this.curLine;
        if (startLine) {
            if (position > curLine.contentStartPos) {
                curLine.checkUnreadOverFlow(position - 1);
                position--;
            } else {
                startLine = false;
            }
        } else {
            // 尝试回退到上一行
            int index = DsonStringBuffer.indexCurLine(lines, curLine);
            if (index > 0) {
                // 回退到上一行的endPos
                curLine = lines.get(index - 1);
                setCurLine(curLine);
                position = curLine.endPos;
                startLine = curLine.hasContent();
            } else {
                // 回退到初始状态
                curLine.checkUnreadFirstLine(firstLine, position);
                setCurLine(null);
                position = -1;
                startLine = false;
            }
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
        try {
            if (reader != null) {
                reader.close();
                reader = null;
            }
        } catch (IOException e) {
            ExceptionUtils.rethrow(e);
        }
    }

    /**
     * 扫描更多的字符，该方法一直读取到缓冲区满，或者行结束
     *
     * @param line 要扫描的行，可能是当前行，也可能是下一行
     */
    private void scanMoreChars(StreamLineInfo line) {
        if (readerEof) {
            return;
        }
        try {
            while (!line.isScanCompleted() && line.remaining() >= 2) {
                int c = reader.read();
                switch (c) {
                    case '\r' -> {
                        line.append((char) c);
                        line.endPos++;

                        c = reader.read();
                        if (c == '\n') { // CRLF line.endPos指向\r位置
                            line.state = STATE_CRLF;
                        } else if (c == -1) { // EOF line.endPos指向一个不可达位置
                            readerEof = true;
                            line.endPos++;
                            line.state = STATE_EOF;
                        } else {
                            line.append((char) c);
                            line.endPos++;
                        }
                    }
                    case '\n' -> {
                        line.append((char) c);
                        line.endPos++;
                        line.state = STATE_LF;
                    }
                    case -1 -> {
                        readerEof = true;
                        line.endPos++;
                        line.state = STATE_EOF;
                    }
                    default -> {
                        line.append((char) c);
                        line.endPos++;
                    }
                }
            }
        } catch (Exception e) {
            InternalUtils.recoveryInterrupt(e);
            ExceptionUtils.rethrow(e);
        }
    }

    private boolean scanNextLine() {
        if (readerEof) {
            return false;
        }

        List<StreamLineInfo> lines = this.lines;
        int ln;
        int startPos;
        if (lines.isEmpty()) {
            ln = 1;
            startPos = 0;
        } else {
            StreamLineInfo lastLine = lines.get(lines.size() - 1);
            ln = lastLine.ln + 1;
            startPos = lastLine.state == STATE_CRLF ?
                    lastLine.endPos + 2 : lastLine.endPos + 1;
        }

        char[] buffer = new char[bufferSizePerLine];
        // startPos指向的是下一个位置，而endPos是在真正读取的时候增加，因此endPos需要回退一个位置
        StreamLineInfo tempLine = new StreamLineInfo(ln, startPos, startPos - 1, LheadType.APPEND, startPos);
        tempLine.buffer = buffer;

        if (jsonLike) {
            scanMoreChars(tempLine);
        } else {
            // 一直扫描到非注释行的行首位置
            do {
                if (tempLine.isScanCompleted()) {
                    if (tempLine.state == STATE_CRLF) {
                        startPos = tempLine.endPos + 2;
                    } else if (tempLine.state == STATE_LF) {
                        startPos = tempLine.endPos + 1;
                    } else {
                        return false; // eof
                    }
                    tempLine.shiftBuffer(buffer.length);
                    tempLine = new StreamLineInfo(++ln, startPos, startPos - 1, LheadType.APPEND, startPos);
                    tempLine.buffer = buffer;
                    continue;
                }

                scanMoreChars(tempLine);
                int index = tempLine.indexOfNonWhiteSpace();
                if (index == -1) {
                    tempLine.shiftBuffer(tempLine.bufferCount);
                    continue;
                }

                // 出现非空白字符，行首字符出现 -- 需要再读取一部分，以检测缩进的正确性；需要将前面的空白字符移除
                if (index > 0) {
                    tempLine.shiftBuffer(index);
                    scanMoreChars(tempLine);
                }
                LheadType lheadType;
                if (tempLine.isScanCompleted() && tempLine.state != STATE_EOF) { // 最后一个字符是换行符
                    lheadType = DsonTexts.parseLhead(tempLine, index, tempLine.bufferCount - 1);
                } else {
                    lheadType = DsonTexts.parseLhead(tempLine, index, tempLine.bufferCount); // 最后一个字符不是换行符
                }
                if (lheadType == LheadType.COMMENT) {
                    continue;
                }
                int contentStart = startPos + DsonTexts.indexContentStart(tempLine, index, tempLine.bufferCount);

                // 需要继承属性
                StreamLineInfo nextLine = new StreamLineInfo(ln, startPos, tempLine.endPos, lheadType, contentStart);
                nextLine.state = tempLine.state;
                nextLine.buffer = tempLine.buffer;
                nextLine.bufferCount = tempLine.bufferCount;
                nextLine.bufferStartPos = tempLine.bufferStartPos;
                tempLine = nextLine;
                break;
            } while (true);
        }
        lines.add(tempLine);
        return true;
    }

    private static final int STATE_SCAN = 0;
    private static final int STATE_CRLF = 1;
    private static final int STATE_LF = 2;
    private static final int STATE_EOF = 3;

    private static class StreamLineInfo extends LineInfo implements CharSequence {

        /** 当前扫描状态 */
        int state = STATE_SCAN;
        /**
         * 使用全局buffer存在多方面的问题，主要与unread相关。
         * 1.由于dson存在行首，标准dson文本下的空白行是需要被忽略的，如果使用全局的buffer，则空白行也需要被缓存进Buffer
         * 2.由于dson支持注释，如果说空白行是意外情况，出现注释则是正常的情况，如果使用全局的buffer，注释行也需要缓存进Buffer
         * 3.使用全局buffer的代码很是复杂，不论是扫描下一行还是回退到上一行。
         * 问题1、2要求我们使用不连续的Buffer，而不连续的Buffer也可以降低代码的复杂度。
         */
        char[] buffer;
        /** buffer的有效字符数 */
        int bufferCount;
        /** buffer全局位置 */
        int bufferStartPos;

        public StreamLineInfo(int ln, int startPos, int endPos, LheadType lheadType, int contentStartPos) {
            super(ln, startPos, endPos, lheadType, contentStartPos);
            this.bufferStartPos = startPos;
        }

        public boolean isScanCompleted() {
            return state != STATE_SCAN;
        }

        @Override
        public boolean hasContent() {
            return state != STATE_SCAN ? contentStartPos < endPos : contentStartPos <= endPos;
        }

        /** bufferEndPos是不可达位置，在for循环迭代时应该使用小于比较 */
        public int bufferEndPos() {
            return bufferStartPos + bufferCount;
        }

        public int remaining() {
            return buffer.length - bufferCount;
        }

        public void append(char c) {
            buffer[bufferCount++] = c;
        }

        public char charAtPosition(int position) {
            return buffer[position - bufferStartPos];
        }

        public int indexOfNonWhiteSpace() {
            char[] buffer = this.buffer;
            for (int i = 0, length = bufferCount; i < length; i++) {
                if (!Character.isWhitespace(buffer[i])) {
                    return i;
                }
            }
            return -1;
        }

        /**
         * 丢弃当前行已读取的部分字节，但要保留一定量的字符以支持回退
         *
         * @param position 该位置的字符需要保留
         */
        public void discardReadChars(int position) {
            if (position - bufferStartPos >= buffer.length * 0.75f) {
                shiftBuffer(buffer.length / 2);
            }
        }

        /** 强制丢弃部分字节 */
        public void shiftBuffer(int shiftCount) {
            if (shiftCount <= 0) {
                return;
            }
            if (shiftCount >= bufferCount) {
//                Arrays.fill(buffer, (char) 0);
                bufferStartPos += bufferCount;
                bufferCount = 0;
            } else {
                System.arraycopy(buffer, shiftCount, buffer, 0, buffer.length - shiftCount);
//                Arrays.fill(buffer, shiftCount, buffer.length, (char) 0);
                bufferCount -= shiftCount;
                bufferStartPos += shiftCount;
            }
        }

        public void checkUnreadOverFlow(int position) {
            if (position < bufferStartPos) {
                throw new DsonParseException("BufferOverFlow, caused by unread, pos: " + position);
            }
        }

        public void checkUnreadFirstLine(@Nullable StreamLineInfo firstLine, int position) {
            if (this != firstLine) {
                throw new DsonParseException("BufferOverFlow, caused by unread, pos: " + position);
            }
        }

        @Override
        public int length() {
            return bufferCount;
        }

        @Override
        public char charAt(int index) {
            return buffer[index];
        }

        @Nonnull
        @Override
        public CharSequence subSequence(int start, int end) {
            return new String(buffer, start, end - start);
        }

        @Nonnull
        @Override
        public String toString() {
            return "StreamLineInfo{" +
                    "ln=" + ln +
                    ", startPos=" + startPos +
                    ", endPos=" + endPos +
                    ", lheadType=" + lheadType +
                    ", contentStartPos=" + contentStartPos +
                    ", state=" + state +
                    ", buffer='" + encodeBuffer() + "'" +
                    ", bufferCount=" + bufferCount +
                    ", bufferStartPos=" + bufferStartPos +
                    '}';
        }

        private String encodeBuffer() {
            if (buffer == null) {
                return "";
            } else {
                return new String(buffer, 0, bufferCount);
            }
        }
    }

}