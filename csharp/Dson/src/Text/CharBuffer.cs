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

using System;
using System.IO;
using Wjybxx.Commons.Collections;
using Wjybxx.Dson.IO;

namespace Wjybxx.Dson.Text;

internal class CharBuffer
{
    internal char[] buffer;
    internal int ridx;
    internal int widx;

    internal CharBuffer(int length) {
        this.buffer = new char[length];
    }

    internal CharBuffer(char[] buffer) {
        this.buffer = buffer ?? throw new ArgumentNullException(nameof(buffer));
    }

    public int Capacity => buffer.Length;

    public bool IsReadable => ridx < widx;

    public bool IsWritable => widx < buffer.Length;

    public int WritableChars => buffer.Length - widx;

    public int ReadableChars => Math.Max(0, widx - ridx);

    /** Length为可读字节数 */
    public int Length => Math.Max(0, widx - ridx);

    public char CharAt(int index) {
        return buffer[ridx + index];
    }

    #region 读写

    public char Read() {
        if (ridx == widx) throw new InternalBufferOverflowException();
        return buffer[ridx++];
    }

    public void Unread() {
        if (ridx == 0) throw new InternalBufferOverflowException();
        ridx--;
    }

    public void Write(char c) {
        if (widx == buffer.Length) {
            throw new InternalBufferOverflowException();
        }
        buffer[widx++] = c;
    }

    public void Write(char[] chars) {
        if (chars.Length == 0) {
            return;
        }
        if (widx + chars.Length > buffer.Length) {
            throw new InternalBufferOverflowException();
        }
        Array.Copy(chars, 0, buffer, widx, chars.Length);
        widx += chars.Length;
    }

    public void Write(char[] chars, int offset, int len) {
        if (len == 0) {
            return;
        }
        BinaryUtils.CheckBuffer(chars.Length, offset, len);
        if (widx + len > buffer.Length) {
            throw new InternalBufferOverflowException();
        }
        Array.Copy(chars, offset, buffer, widx, len);
        widx += len;
    }

    /**
     * 将给定buffer中的可读字符写入到当前buffer中
     * 给定的buffer的读索引将更新，当前buffer的写索引将更新
     *
     * @return 写入的字符数；返回0时可能是因为当前buffer已满，或给定的buffer无可读字符
     */
    public int Write(CharBuffer charBuffer) {
        int n = Math.Min(WritableChars, charBuffer.ReadableChars);
        if (n == 0) {
            return 0;
        }
        Write(charBuffer.buffer, charBuffer.ridx, n);
        charBuffer.AddRidx(n);
        return n;
    }

    #endregion

    #region 索引调整

    public void AddRidx(int count) {
        SetRidx(ridx + count);
    }

    public void AddWidx(int count) {
        SetWidx(widx + count);
    }

    public void SetRidx(int ridx) {
        if (ridx < 0 || ridx > widx) {
            throw new ArgumentException("ridx overflow");
        }
        this.ridx = ridx;
    }

    public void SetWidx(int widx) {
        if (widx < ridx || widx > buffer.Length) {
            throw new ArgumentException("widx overflow");
        }
        this.widx = widx;
    }

    public void SetIndexes(int ridx, int widx) {
        if (ridx < 0 || ridx > widx) {
            throw new ArgumentException("ridx overflow");
        }
        if (widx > buffer.Length) {
            throw new ArgumentException("widx overflow");
        }
        this.ridx = ridx;
        this.widx = widx;
    }

    #endregion

    #region 容量调整

    public void Shift(int shiftCount) {
        if (shiftCount <= 0) {
            return;
        }
        if (shiftCount >= buffer.Length) {
            ridx = 0;
            widx = 0;
        } else {
            Array.Copy(buffer, shiftCount, buffer, 0, buffer.Length - shiftCount);
            ridx = Math.Max(0, ridx - shiftCount);
            widx = Math.Max(0, widx - shiftCount);
        }
    }

    public void Grow(int capacity) {
        char[] buffer = this.buffer;
        if (capacity <= buffer.Length) {
            return;
        }
        this.buffer = CollectionUtil.CopyOf(this.buffer, 0, capacity);
    }

    #endregion

    public override string ToString() {
        return "CharBuffer{" +
               "buffer='" + EncodeBuffer() + "'" +
               ", ridx=" + ridx +
               ", widx=" + widx +
               '}';
    }

    private string EncodeBuffer() {
        if (ridx >= widx) {
            return "";
        }
        return new string(buffer, ridx, Math.Max(0, widx - ridx));
    }
}