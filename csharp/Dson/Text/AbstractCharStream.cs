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

using System.Diagnostics;

namespace Dson.Text;

public abstract class AbstractCharStream : DsonCharStream
{
    protected readonly DsonMode DsonMode;
    private readonly List<LineInfo> _lines = new List<LineInfo>();
    private LineInfo? _curLine;
    private bool _readingContent = false;
    private int _position = -1;
    private bool _eof = false;

    internal AbstractCharStream(DsonMode dsonMode) {
        this.DsonMode = dsonMode;
    }

    protected void AddLine(LineInfo lineInfo) {
        if (lineInfo == null) throw new ArgumentNullException(nameof(lineInfo));
        _lines.Add(lineInfo);
    }

    public int read() {
        if (isClosed()) throw new DsonParseException("Trying to read after closed");
        if (_eof) throw new DsonParseException("Trying to read past eof");

        LineInfo curLine = this._curLine;
        if (curLine == null) {
            if (_lines.Count == 0 && !scanNextLine()) {
                _eof = true;
                return -1;
            }
            curLine = _lines[0];
            onReadNextLine(curLine);
            return -2;
        }
        // 到达当前扫描部分的尾部，扫描更多的字符 - 不测试readingContent也没问题
        if (_position == curLine.endPos && !curLine.isScanCompleted()) {
            scanMoreChars(curLine); // 要么读取到一个输入，要么行扫描完毕
            Debug.Assert(_position < curLine.endPos || curLine.isScanCompleted());
        }
        if (curLine.isScanCompleted()) {
            if (_readingContent) {
                if (_position >= curLine.lastReadablePosition()) { // 读完或已在行尾(unread)
                    return onReadEndOfLine(curLine);
                }
                else {
                    _position++;
                }
            }
            else if (curLine.hasContent()) {
                _readingContent = true;
            }
            else {
                return onReadEndOfLine(curLine);
            }
        }
        else {
            if (_readingContent) {
                _position++;
            }
            else {
                _readingContent = true;
            }
        }
        return charAt(curLine, _position);
    }

    private int onReadEndOfLine(LineInfo curLine) {
        // 这里不可以修改position，否则unread可能出错
        if (curLine.state == LineInfo.STATE_EOF) {
            _eof = true;
            return -1;
        }
        int index = indexOfCurLine(_lines, curLine);
        if (index + 1 == _lines.Count && !scanNextLine()) {
            _eof = true;
            return -1;
        }
        curLine = _lines[index + 1];
        onReadNextLine(curLine);
        return -2;
    }

    private void onReadNextLine(LineInfo nextLine) {
        Debug.Assert(nextLine.isScanCompleted() || nextLine.hasContent());
        this._curLine = nextLine;
        this._readingContent = false;
        if (nextLine.hasContent()) {
            this._position = nextLine.contentStartPos;
        }
        else {
            this._position = nextLine.startPos;
        }
        discardReadLines(_lines, nextLine); // 清除部分缓存
    }

    private void onBackToPreLine(LineInfo preLine) {
        Debug.Assert(preLine.isScanCompleted());
        this._curLine = preLine;
        if (preLine.hasContent()) {
            // 有内容的情况下，需要回退到上一行最后一个字符的位置，否则继续unread会出错
            this._position = preLine.lastReadablePosition();
            this._readingContent = true;
        }
        else {
            // 无内容的情况下回退到startPos，和read保持一致
            this._position = preLine.startPos;
            this._readingContent = false;
        }
    }

    public int unread() {
        if (_eof) {
            _eof = false;
            return -1;
        }
        LineInfo curLine = this._curLine;
        if (curLine == null) {
            throw new InvalidOperationException("read must be called before unread.");
        }
        // 当前行回退 -- 需要检测是否回退到bufferStartPos之前
        if (_readingContent) {
            if (_position > curLine.contentStartPos) {
                checkUnreadOverFlow(_position - 1);
                _position--;
            }
            else {
                _readingContent = false;
            }
            return 0;
        }
        // 尝试回退到上一行，需要检测上一行的最后一个可读字符是否溢出
        int index = indexOfCurLine(_lines, curLine);
        if (index > 0) {
            LineInfo preLine = _lines[index - 1];
            if (preLine.hasContent()) {
                checkUnreadOverFlow(preLine.lastReadablePosition());
            }
            else {
                checkUnreadOverFlow(preLine.startPos);
            }
            onBackToPreLine(preLine);
            return -2;
        }
        else {
            if (curLine.ln != getStartLn()) {
                throw bufferOverFlow(_position);
            }
            // 回退到初始状态
            this._curLine = null;
            this._readingContent = false;
            this._position = -1;
            return 0;
        }
    }

    public void skipLine() {
        LineInfo curLine = this._curLine;
        if (curLine == null) throw new InvalidOperationException();
        while (!curLine.isScanCompleted()) {
            _position = curLine.endPos;
            scanMoreChars(curLine);
        }
        if (curLine.hasContent()) {
            _readingContent = true;
            _position = curLine.lastReadablePosition();
        }
    }

    public int getPosition() {
        return _position;
    }

    public LineInfo? getCurLine() {
        return _curLine;
    }

    //

    protected static int indexOfCurLine(List<LineInfo> lines, LineInfo curLine) {
        return curLine.ln - lines[0].ln;
    }

    protected static DsonParseException bufferOverFlow(int position) {
        return new DsonParseException("BufferOverFlow, caused by unread, pos: " + position);
    }

    protected bool isReadingContent() {
        return _readingContent;
    }

    protected bool isEof() {
        return _eof;
    }

    protected int getStartLn() {
        return 1;
    }

    /** 丢弃部分已读的行，减少内存占用 */
    protected void discardReadLines(List<LineInfo> lines, LineInfo curLine) {
        if (curLine == null) {
            return;
        }
        int idx = indexOfCurLine(lines, curLine);
        if (idx >= 10) {
            lines.RemoveRange(0, 5);
        }
    }

    /// <summary>
    /// 当前流是否已处于关闭状态
    /// </summary>
    /// <returns>如果已关闭则返回true</returns>
    protected abstract bool isClosed();

    /// <summary>
    /// 获取Line在全局位置的字符
    /// </summary>
    /// <param name="curLine">当前读取的行</param>
    /// <param name="position">全局位置</param>
    /// <returns></returns>
    protected abstract int charAt(LineInfo curLine, int position);

    /// <summary>
    /// 检测是否可以回退到指定位置
    /// </summary>
    /// <param name="position"></param>
    /// <exception cref="DsonParseException">如果不可回退到指定位置</exception>
    protected abstract void checkUnreadOverFlow(int position);

    /// <summary>
    /// 丢弃指定位置以前已读的字节
    /// </summary>
    /// <param name="position"></param>
    public virtual void discardReadChars(int position) {
    }

    /// <summary>
    /// 扫描更多的字符
    /// 注意：要么读取到一个输入，要么行扫描完毕
    /// </summary>
    /// <param name="line">要扫描的行，可能是当前行，也可能是下一行</param>
    /// <exception cref="DsonParseException">如果缓冲区已满</exception>
    protected abstract void scanMoreChars(LineInfo line);

    /// <summary>
    /// 尝试扫描下一行（可以扫描多行）
    /// </summary>
    /// <returns>如果扫描到新的一行则返回true</returns>
    protected abstract bool scanNextLine();

    public abstract void Dispose();
}