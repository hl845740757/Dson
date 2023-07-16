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

import java.io.IOException;
import java.io.Reader;
import java.util.Objects;

/**
 * @author wjybxx
 * date - 2023/6/5
 */
final class BufferedCharStream extends AbstractCharStream {

    private static final int MIN_BUFFER_SIZE = 32;
    /** 行首前面超过1000个空白字符是有问题的 */
    private static final int MAX_BUFFER_SIZE = 1024;

    private Reader reader;
    private CharBuffer buffer;
    /** reader批量读取数据到该buffer，然后再读取到当前buffer -- 缓冲的缓冲，减少io操作 */
    private CharBuffer nextBuffer;

    /** buffer全局开始位置 */
    public int bufferStartPos;
    /** reader是否已到达文件尾部 -- 部分reader在到达文件尾部的时候不可继续读 */
    private boolean readerEof;

    BufferedCharStream(boolean jsonLike, Reader reader) {
        this(jsonLike, reader, MIN_BUFFER_SIZE * 2);
    }

    BufferedCharStream(boolean jsonLike, Reader reader, int bufferSize) {
        super(jsonLike);
        Objects.requireNonNull(reader);
        bufferSize = Math.max(MIN_BUFFER_SIZE, bufferSize);

        this.reader = reader;
        this.buffer = new CharBuffer(new char[bufferSize]);
        this.nextBuffer = new CharBuffer(new char[64]);
    }

    @Override
    public void close() {
        try {
            if (reader != null) {
                reader.close();
                reader = null;
            }
            buffer = null;
            nextBuffer = null;
        } catch (IOException e) {
            ExceptionUtils.rethrow(e);
        }
    }

    @Override
    protected boolean isClosed() {
        return reader == null;
    }

    @Override
    protected int charAt(LineInfo curLine, int position) {
        int ridx = position - bufferStartPos;
        return buffer.charAt(ridx);
    }

    @Override
    protected void checkUnreadOverFlow(LineInfo line, int position) {
        int ridx = position - bufferStartPos;
        if (ridx < 0 || ridx >= buffer.widx) {
            throw bufferOverFlow(position);
        }
    }

    @Override
    public void discardReadChars(int position) {
        if (position <= 0 || position >= getPosition()) {
            return;
        }
        int shiftCount = position - bufferStartPos - 1;
        if (shiftCount > 0) {
            buffer.shift(shiftCount);
            bufferStartPos += shiftCount;
        }
    }

    private void discardReadChars() {
        CharBuffer buffer = this.buffer;
        // 已读部分达75%时丢弃50%(保留最近的25%)；这里不根据具体的数字来进行丢弃，减少不必要的数组拷贝
        if (getPosition() - bufferStartPos >= buffer.capacity() * 0.75f) {
            discardReadChars((int) (bufferStartPos + buffer.capacity() * 0.25d));
        }
        // 如果可写空间不足，则尝试扩容
        if (buffer.writableChars() <= 4
                && buffer.capacity() < MAX_BUFFER_SIZE) {
            growUp(buffer);
        }
    }

    private void growUp(CharBuffer charBuffer) {
        int capacity = Math.min(MAX_BUFFER_SIZE, charBuffer.capacity() * 2);
        charBuffer.grow(capacity);
    }

    /** 该方法一直读到指定行读取完毕，或缓冲区满(不一定扩容) */
    @Override
    protected void scanMoreChars(LineInfo line) {
        discardReadChars();
        CharBuffer buffer = this.buffer;
        CharBuffer nextBuffer = this.nextBuffer;
        try {
            // >= 2 是为了处理\r\n换行符，避免读入单个\r不知如何处理
            while (!line.isScanCompleted() && buffer.writableChars() >= 2) {
                readToBuffer(nextBuffer);
                while (nextBuffer.isReadable() && buffer.writableChars() >= 2) {
                    char c = nextBuffer.read();
                    line.endPos++;
                    buffer.write(c);

                    if (c == '\n') { // LF
                        line.state = LineInfo.STATE_LF;
                        return;
                    }
                    if (c == '\r') {
                        // 再读取一次，以判断\r\n
                        if (!nextBuffer.isReadable()) {
                            readToBuffer(nextBuffer);
                        }
                        if (!nextBuffer.isReadable()) {
                            assert readerEof;
                            line.state = LineInfo.STATE_EOF;
                            return;
                        }
                        c = nextBuffer.read();
                        line.endPos++;
                        buffer.write(c);
                        if (c == '\n') { // CRLF
                            line.state = LineInfo.STATE_CRLF;
                            return;
                        }
                    }
                }
                if (readerEof && !nextBuffer.isReadable()) {
                    line.state = LineInfo.STATE_EOF;
                    return;
                }
            }
        } catch (IOException e) {
            InternalUtils.recoveryInterrupt(e.getCause());
            ExceptionUtils.rethrow(e);
        }
    }

    private void readToBuffer(CharBuffer nextBuffer) throws IOException {
        if (!readerEof) {
            if (nextBuffer.ridx >= nextBuffer.capacity() / 2) {
                nextBuffer.shift(nextBuffer.ridx);
            }
            int len = nextBuffer.writableChars();
            if (len <= 0) {
                return;
            }
            int n = reader.read(nextBuffer.buffer, nextBuffer.widx, len);
            if (n == -1) {
                readerEof = true;
            } else {
                nextBuffer.addWidx(n);
            }
        }
    }

    @Override
    protected boolean scanNextLine() {
        if (readerEof && !nextBuffer.isReadable()) {
            return false;
        }
        LineInfo curLine = getCurLine();
        final int ln;
        final int startPos;
        if (curLine == null) {
            ln = getStartLn();
            startPos = 0;
        } else {
            ln = curLine.ln + 1;
            startPos = curLine.endPos + 1;
        }

        // startPos指向的是下一个位置，而endPos是在scan的时候增加，因此endPos需要回退一个位置
        LineInfo tempLine = new LineInfo(ln, startPos, startPos - 1, LheadType.APPEND, startPos);
        scanMoreChars(tempLine);
        if (tempLine.startPos > tempLine.endPos) { // 无效行，没有输入
            return false;
        }
        if (jsonLike) {
            if (startPos > tempLine.lastReadablePosition()) { // 空行(仅换行符)
                tempLine = new LineInfo(ln, startPos, tempLine.endPos, LheadType.APPEND, -1);
            }
            addLine(tempLine);
            return true;
        }

        CharBuffer buffer = this.buffer;
        int bufferStartPos = this.bufferStartPos;
        // 必须出现行首，或扫描结束
        int headPos = -1;
        int indexStartPos = startPos;
        while (true) {
            if (headPos == -1) {
                for (; indexStartPos <= tempLine.endPos; indexStartPos++) {
                    int ridx = indexStartPos - bufferStartPos;
                    if (!DsonTexts.isIndentChar(buffer.charAt(ridx))) {
                        headPos = indexStartPos;
                        break;
                    }
                }
            }
            if (tempLine.isScanCompleted()) {
                break;
            }
            // 不足以判定行首或内容的开始，需要继续读 -- 要准备更多的空间
            if (headPos == -1 || headPos + 2 > tempLine.endPos) {
                if (buffer.capacity() >= MAX_BUFFER_SIZE) {
                    throw new DsonParseException("BufferOverFlow, caused by scanNextLine, pos: " + getPosition());
                }
                if (getPosition() - bufferStartPos >= MIN_BUFFER_SIZE) {
                    discardReadChars(bufferStartPos + MIN_BUFFER_SIZE / 2);
                }
                growUp(buffer);
                scanMoreChars(tempLine);
                continue;
            }
            break;
        }

        int state = tempLine.state; // 可能已完成，也可能未完成
        int lastReadablePos = tempLine.lastReadablePosition();
        if (headPos >= startPos && headPos <= lastReadablePos) {
            String label = Character.toString(buffer.charAt(headPos - bufferStartPos));
            LheadType lheadType = LheadType.forLabel(label);
            if (lheadType == null) {
                throw new DsonParseException("Unknown head %s, pos: %d".formatted(label, headPos));
            }
            // 检查缩进
            if (headPos + 1 <= lastReadablePos && buffer.charAt(headPos + 1 - bufferStartPos) != ' ') {
                throw new DsonParseException("space is required, head %s, pos: %d".formatted(label, headPos));
            }
            // 确定内容开始位置
            int contentStartPos = -1;
            if (headPos + 2 <= lastReadablePos) {
                contentStartPos = headPos + 2;
            }
            tempLine = new LineInfo(ln, tempLine.startPos, tempLine.endPos, lheadType, contentStartPos);
            tempLine.state = state;
        } else {
            assert tempLine.isScanCompleted();
            tempLine = new LineInfo(ln, startPos, tempLine.endPos, LheadType.COMMENT, -1);
            tempLine.state = state;
        }
        addLine(tempLine);
        return true;
    }

}