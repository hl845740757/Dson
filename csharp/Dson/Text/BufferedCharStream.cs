using System.Diagnostics;

namespace Dson.Text;

class BufferedCharStream : AbstractCharStream
{
    private const int MinBufferSize = 32;
    /** 行首前面超过1000个空白字符是有问题的 */
    private const int MaxBufferSize = 1024;

#nullable disable
    private StreamReader _reader;
    private readonly bool _autoClose;
#nullable enable

    private CharBuffer _buffer;
    /** reader批量读取数据到该buffer，然后再读取到当前buffer -- 缓冲的缓冲，减少io操作 */
    private CharBuffer _nextBuffer;

    /** buffer全局开始位置 */
    private int _bufferStartPos;
    /** reader是否已到达文件尾部 -- 部分reader在到达文件尾部的时候不可继续读 */
    private bool _readerEof;

    public BufferedCharStream(StreamReader reader, DsonMode dsonMode,
                              int bufferSize = 64, bool autoClose = true) : base(dsonMode) {
        this._reader = reader ?? throw new ArgumentNullException(nameof(reader));
        this._buffer = new CharBuffer(Math.Max(MinBufferSize, bufferSize));
        this._nextBuffer = new CharBuffer(64);
        this._autoClose = autoClose;
    }

    public override void Dispose() {
        if (_reader != null && _autoClose) {
            _reader.Dispose();
            _reader = null;
        }
        _buffer = null!;
        _nextBuffer = null!;
    }

    protected override bool IsClosed() {
        return _reader == null;
    }

    protected override int CharAt(LineInfo curLine, int position) {
        int ridx = position - _bufferStartPos;
        return _buffer.CharAt(ridx);
    }

    protected override void CheckUnreadOverFlow(int position) {
        int ridx = position - _bufferStartPos;
        if (ridx < 0 || ridx >= _buffer.Widx) {
            throw BufferOverFlow(position);
        }
    }

    public override void DiscardReadChars(int position) {
        if (position <= 0 || position >= Position) {
            return;
        }
        int shiftCount = position - _bufferStartPos - 1;
        if (shiftCount > 0) {
            _buffer.Shift(shiftCount);
            _bufferStartPos += shiftCount;
        }
    }

    private void DiscardReadChars() {
        CharBuffer buffer = this._buffer;
        // 已读部分达75%时丢弃50%(保留最近的25%)；这里不根据具体的数字来进行丢弃，减少不必要的数组拷贝
        if (Position - _bufferStartPos >= buffer.Capacity * 0.75f) {
            DiscardReadChars((int)(_bufferStartPos + buffer.Capacity * 0.25d));
        }
        // 如果可写空间不足，则尝试扩容
        if (buffer.WritableChars <= 4
            && buffer.Capacity < MaxBufferSize) {
            GrowUp(buffer);
        }
    }

    private void GrowUp(CharBuffer charBuffer) {
        int capacity = Math.Min(MaxBufferSize, charBuffer.Capacity * 2);
        charBuffer.Grow(capacity);
    }

    /** 该方法一直读到指定行读取完毕，或缓冲区满(不一定扩容) */
    protected override void ScanMoreChars(LineInfo line) {
        DiscardReadChars();
        CharBuffer buffer = this._buffer;
        CharBuffer nextBuffer = this._nextBuffer;
        try {
            // >= 2 是为了处理\r\n换行符，避免读入单个\r不知如何处理
            while (!line.IsScanCompleted() && buffer.WritableChars >= 2) {
                ReadToBuffer(nextBuffer);
                while (nextBuffer.IsReadable && buffer.WritableChars >= 2) {
                    char c = nextBuffer.Read();
                    line.EndPos++;
                    buffer.Write(c);

                    if (c == '\n') { // LF
                        line.State = LineInfo.StateLf;
                        return;
                    }
                    if (c == '\r') {
                        // 再读取一次，以判断\r\n
                        if (!nextBuffer.IsReadable) {
                            ReadToBuffer(nextBuffer);
                        }
                        if (!nextBuffer.IsReadable) {
                            Debug.Assert(_readerEof);
                            line.State = LineInfo.StateEof;
                            return;
                        }
                        c = nextBuffer.Read();
                        line.EndPos++;
                        buffer.Write(c);
                        if (c == '\n') { // CRLF
                            line.State = LineInfo.StateCrlf;
                            return;
                        }
                    }
                }
                if (_readerEof && !nextBuffer.IsReadable) {
                    line.State = LineInfo.StateEof;
                    return;
                }
            }
        }
        catch (ThreadInterruptedException _) {
            Thread.CurrentThread.Interrupt(); // 恢复中断
        }
    }

    private void ReadToBuffer(CharBuffer nextBuffer) {
        if (!_readerEof) {
            if (nextBuffer.Ridx >= nextBuffer.Capacity / 2) {
                nextBuffer.Shift(nextBuffer.Ridx);
            }
            int len = nextBuffer.WritableChars;
            if (len <= 0) {
                return;
            }
            int n = _reader.Read(nextBuffer.Buffer, nextBuffer.Widx, len);
            if (n == -1) {
                _readerEof = true;
            }
            else {
                nextBuffer.AddWidx(n);
            }
        }
    }

    protected override bool ScanNextLine() {
        if (_readerEof && !_nextBuffer.IsReadable) {
            return false;
        }
        LineInfo? curLine = CurLine;
        int ln;
        int startPos;
        if (curLine == null) {
            ln = FirstLn;
            startPos = 0;
        }
        else {
            ln = curLine.Ln + 1;
            startPos = curLine.EndPos + 1;
        }

        // startPos指向的是下一个位置，而endPos是在scan的时候增加，因此endPos需要回退一个位置
        LineInfo tempLine = new LineInfo(ln, startPos, startPos - 1, LineHead.Append, startPos);
        ScanMoreChars(tempLine);
        if (tempLine.StartPos > tempLine.EndPos) { // 无效行，没有输入
            return false;
        }
        if (DsonMode == DsonMode.Relaxed) {
            if (startPos > tempLine.LastReadablePosition()) { // 空行(仅换行符)
                tempLine = new LineInfo(ln, startPos, tempLine.EndPos, LineHead.Append, -1);
            }
            AddLine(tempLine);
            return true;
        }

        CharBuffer buffer = this._buffer;
        int bufferStartPos = this._bufferStartPos;
        // 必须出现行首，或扫描结束
        int headPos = -1;
        int indexStartPos = startPos;
        while (true) {
            if (headPos == -1) {
                for (; indexStartPos <= tempLine.EndPos; indexStartPos++) {
                    int ridx = indexStartPos - bufferStartPos;
                    if (!DsonTexts.IsIndentChar(buffer.CharAt(ridx))) {
                        headPos = indexStartPos;
                        break;
                    }
                }
            }
            if (tempLine.IsScanCompleted()) {
                break;
            }
            // 不足以判定行首或内容的开始，需要继续读 -- 要准备更多的空间
            if (headPos == -1 || headPos + 2 > tempLine.EndPos) {
                if (buffer.Capacity >= MaxBufferSize) {
                    throw new DsonParseException("BufferOverFlow, caused by scanNextLine, pos: " + Position);
                }
                if (Position - bufferStartPos >= MinBufferSize) {
                    DiscardReadChars(bufferStartPos + MinBufferSize / 2);
                }
                GrowUp(buffer);
                ScanMoreChars(tempLine);
                continue;
            }
            break;
        }

        int state = tempLine.State; // 可能已完成，也可能未完成
        int lastReadablePos = tempLine.LastReadablePosition();
        if (headPos >= startPos && headPos <= lastReadablePos) {
            String label = buffer.CharAt(headPos - bufferStartPos).ToString();
            LineHead? lineHead = DsonTexts.LineHeadOfLabel(label);
            if (!lineHead.HasValue) {
                throw new DsonParseException($"Unknown head {label}, pos: {headPos}");
            }
            // 检查缩进
            if (headPos + 1 <= lastReadablePos && buffer.CharAt(headPos + 1 - bufferStartPos) != ' ') {
                throw new DsonParseException($"space is required, head {{label}}, pos: {headPos}");
            }
            // 确定内容开始位置
            int contentStartPos = -1;
            if (headPos + 2 <= lastReadablePos) {
                contentStartPos = headPos + 2;
            }
            tempLine = new LineInfo(ln, tempLine.StartPos, tempLine.EndPos, lineHead.Value, contentStartPos);
            tempLine.State = state;
        }
        else {
            Debug.Assert(tempLine.IsScanCompleted());
            tempLine = new LineInfo(ln, startPos, tempLine.EndPos, LineHead.Comment, -1);
            tempLine.State = state;
        }
        AddLine(tempLine);
        return true;
    }
}