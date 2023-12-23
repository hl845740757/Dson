#region LICENSE

//  Copyright 2023 wjybxx(845740757@qq.com)
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

using System.Text;

#pragma warning disable CS1591
namespace Wjybxx.Dson.Text;

/// <summary>
/// Dson文本行扫描信息
/// </summary>
public class LineInfo
{
    /** 扫描中 */
    public const int StateScan = 0;
    /** 扫描到'\n'换行符 */
    public const int StateLf = 1;
    /** 扫描到'\r\n'换行符 */
    public const int StateCrlf = 2;
    /** 扫描到文件尾 */
    public const int StateEof = 3;

    /** 行号 */
    public readonly int Ln;
    /** 行全局起始位置， 0-based */
    public readonly int StartPos;
    /**
     * 行结束位置（全局），0-based
     * 1.如果换行符是\r\n，则是\n的位置；
     * 2.如果换行符是\n，则是\n的位置；
     * 3.eof的情况下，是最后一个字符的位置 --换行结束的情况下，eof出现在读取下一行的时候
     * 4.start和end相等时表示空行；
     */
    public int EndPos;
    /** 行在字符流中的状态 -- endPos是否到达行尾 */
    public int State = StateScan;

    /** 行首类型 */
    public readonly LineHead LineHead;
    /**
     * 内容全局起始位置 -- -1表示无内容
     * 由于文件可能是没有行首的，因此不能记录行首的开始位置;
     */
    public readonly int ContentStartPos;

    public LineInfo(int ln, int startPos, int endPos, LineHead lineHead, int contentStartPos) {
        this.Ln = ln;
        this.StartPos = startPos;
        this.EndPos = endPos;
        this.LineHead = lineHead;
        this.ContentStartPos = contentStartPos;
    }

    /** 当前行是否已扫描完成 */
    public bool IsScanCompleted() {
        return State != StateScan;
    }

    /** 最后一个可读取的位置 */
    public int LastReadablePosition() {
        return LastReadablePosition(State, EndPos);
    }

    /** 最后一个可读取的位置 -- 不包含换行符；可能小于startPos */
    public static int LastReadablePosition(int state, int endPos) {
        if (state == StateLf) {
            return endPos - 1;
        }
        if (state == StateCrlf) {
            return endPos - 2;
        }
        return endPos;
    }

    /** 当前行是否有内容 */
    public bool HasContent() {
        return ContentStartPos != -1;
    }

    /** 当前已扫描部分长度 */
    public int LineLength() {
        if (EndPos < StartPos) {
            return 0;
        }
        return EndPos - StartPos + 1;
    }

    #region equals

    public override bool Equals(Object o) {
        return this == o;
    }

    public override int GetHashCode() {
        return Ln;
    }

    #endregion

    public override string ToString() {
        return new StringBuilder(64)
            .Append("LineInfo{")
            .Append("ln=").Append(Ln)
            .Append(", startPos=").Append(StartPos)
            .Append(", endPos=").Append(EndPos)
            .Append(", state=").Append(State)
            .Append(", lineHead=").Append(LineHead)
            .Append(", contentStartPos=").Append(ContentStartPos)
            .Append('}').ToString();
    }
}