﻿#region LICENSE

//  Copyright 2023-2024 wjybxx(845740757@qq.com)
// 
//  Licensed under the Apache License, Version 2.0 (the "License");
//  you may not use this file except in compliance with the License.
//  You may obtain a copy of the License at
// 
//      http://www.apache.org/licenses/LICENSE-2.0
// 
//  Unless required by applicable law or agreed to iBn writing, software
//  distributed under the License is distributed on an "AS IS" BASIS,
//  WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//  See the License for the specific language governing permissions and
//  limitations under the License.

#endregion

using System;

namespace Wjybxx.Dson.Text;

class StringCharStream : AbstractCharStream
{
#nullable disable
    private string _buffer;
#nullable enable

    public StringCharStream(string buffer) {
        this._buffer = buffer ?? throw new ArgumentNullException(nameof(buffer));
        int idx = DsonTexts.IndexOfNonWhitespace(buffer, 0);
        if (idx < 0) {
            _dsonMode = DsonMode.Relaxed;
            SetPosition(buffer.Length);
        } else {
            _dsonMode = DsonTexts.DetectDsonMode(buffer[idx]);
            SetPosition(idx - 1);
        }
    }

    public override void Dispose() {
        _buffer = null;
    }

    protected override bool IsClosed() {
        return _buffer == null;
    }

    protected override int CharAt(LineInfo curLine, int position) {
        return _buffer![position];
    }

    protected override void CheckUnreadOverFlow(int position) {
        if (position < 0 || position >= _buffer!.Length) {
            throw BufferOverFlow(position);
        }
    }

    protected override void ScanMoreChars(LineInfo line) {
    }

    protected override bool ScanNextLine() {
        string buffer = this._buffer;
        int bufferLength = buffer.Length;

        LineInfo? curLine = CurLine;
        int startPos;
        int ln;
        if (curLine == null) {
            ln = FirstLn;
            startPos = 0;
        } else {
            ln = curLine.Ln + 1;
            startPos = curLine.EndPos + 1;
        }
        if (startPos >= bufferLength) {
            return false;
        }

        int state = LineInfo.StateScan;
        int endPos = startPos;
        int headPos = -1;

        for (; endPos < bufferLength; endPos++) {
            char c = buffer[endPos];
            // 需要放在switch-case之前，否则可能漏掉\r的非法head
            if (headPos == -1 && !DsonTexts.IsIndentChar(c)) {
                headPos = endPos;
            }
            if (c == '\n') {
                state = LineInfo.StateLf;
                break;
            }
            if (c == '\r') {
                if (endPos == bufferLength - 1) { // eof
                    state = LineInfo.StateEof;
                    break;
                }
                c = buffer[++endPos];
                if (c == '\n') { // CRLF
                    state = LineInfo.StateCrlf;
                    break;
                }
            }
            if (endPos == bufferLength - 1) { // eof
                state = LineInfo.StateEof;
                break;
            }
        }

        LineHead? lineHead = LineHead.Comment;
        int contentStartPos = -1;
        int lastReadablePos = LineInfo.LastReadablePosition(state, endPos);
        if (_dsonMode == DsonMode.Relaxed) {
            if (startPos <= lastReadablePos) {
                lineHead = LineHead.Append;
                contentStartPos = startPos;
            }
        } else {
            if (headPos >= startPos && headPos <= lastReadablePos) {
                string label = buffer[headPos].ToString();
                lineHead = DsonTexts.LineHeadOfLabel(label);
                if (!lineHead.HasValue) {
                    throw new DsonParseException($"Unknown head {label}, pos: {headPos}");
                }
                // 检查缩进
                if (headPos + 1 <= lastReadablePos && buffer[headPos + 1] != ' ') {
                    throw new DsonParseException($"space is required, head {label}, pos: {headPos}");
                }
                // 确定内容开始位置
                if (headPos + 2 <= lastReadablePos) {
                    contentStartPos = headPos + 2;
                }
            }
        }
        LineInfo tempLine = new LineInfo(ln, startPos, endPos, lineHead.Value, contentStartPos);
        tempLine.State = state;
        AddLine(tempLine);
        return true;
    }
}