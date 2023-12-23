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

using Wjybxx.Commons.Collections;
using Wjybxx.Dson.IO;

namespace Wjybxx.Dson.Text;

internal class CharBuffer
{
    internal char[] Buffer;
    internal int Ridx;
    internal int Widx;

    internal CharBuffer(int length) {
        this.Buffer = new char[length];
    }

    internal CharBuffer(char[] buffer) {
        this.Buffer = buffer ?? throw new ArgumentNullException(nameof(buffer));
    }

    public int Capacity => Buffer.Length;

    public bool IsReadable => Ridx < Widx;

    public bool IsWritable => Widx < Buffer.Length;

    public int WritableChars => Buffer.Length - Widx;

    public int ReadableChars => Math.Max(0, Widx - Ridx);

    /** Length为可读字节数 */
    public int Length => Math.Max(0, Widx - Ridx);

    public char CharAt(int index) {
        return Buffer[Ridx + index];
    }

    #region 读写

    public char Read() {
        if (Ridx == Widx) throw new InternalBufferOverflowException();
        return Buffer[Ridx++];
    }

    public void Unread() {
        if (Ridx == 0) throw new InternalBufferOverflowException();
        Ridx--;
    }

    public void Write(char c) {
        if (Widx == Buffer.Length) {
            throw new InternalBufferOverflowException();
        }
        Buffer[Widx++] = c;
    }

    public void Write(char[] chars) {
        if (chars.Length == 0) {
            return;
        }
        if (Widx + chars.Length > Buffer.Length) {
            throw new InternalBufferOverflowException();
        }
        Array.Copy(chars, 0, Buffer, Widx, chars.Length);
        Widx += chars.Length;
    }

    public void Write(char[] chars, int offset, int len) {
        if (len == 0) {
            return;
        }
        BinaryUtils.CheckBuffer(chars.Length, offset, len);
        if (Widx + len > Buffer.Length) {
            throw new InternalBufferOverflowException();
        }
        Array.Copy(chars, offset, Buffer, Widx, len);
        Widx += len;
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
        Write(charBuffer.Buffer, charBuffer.Ridx, n);
        charBuffer.AddRidx(n);
        return n;
    }

    #endregion

    #region 索引调整

    public void AddRidx(int count) {
        SetRidx(Ridx + count);
    }

    public void AddWidx(int count) {
        SetWidx(Widx + count);
    }

    public void SetRidx(int ridx) {
        if (ridx < 0 || ridx > Widx) {
            throw new ArgumentException("ridx overflow");
        }
        this.Ridx = ridx;
    }

    public void SetWidx(int widx) {
        if (widx < Ridx || widx > Buffer.Length) {
            throw new ArgumentException("widx overflow");
        }
        this.Widx = widx;
    }

    public void SetIndexes(int ridx, int widx) {
        if (ridx < 0 || ridx > widx) {
            throw new ArgumentException("ridx overflow");
        }
        if (widx > Buffer.Length) {
            throw new ArgumentException("widx overflow");
        }
        this.Ridx = ridx;
        this.Widx = widx;
    }

    #endregion

    #region 容量调整

    public void Shift(int shiftCount) {
        if (shiftCount <= 0) {
            return;
        }
        if (shiftCount >= Buffer.Length) {
            Ridx = 0;
            Widx = 0;
        }
        else {
            Array.Copy(Buffer, shiftCount, Buffer, 0, Buffer.Length - shiftCount);
            Ridx = Math.Max(0, Ridx - shiftCount);
            Widx = Math.Max(0, Widx - shiftCount);
        }
    }

    public void Grow(int capacity) {
        char[] buffer = this.Buffer;
        if (capacity <= buffer.Length) {
            return;
        }
        this.Buffer = CollectionUtil.CopyOf(this.Buffer, 0, capacity);
    }

    #endregion

    public override string ToString() {
        return "CharBuffer{" +
               "buffer='" + EncodeBuffer() + "'" +
               ", ridx=" + Ridx +
               ", widx=" + Widx +
               '}';
    }

    private string EncodeBuffer() {
        if (Ridx >= Widx) {
            return "";
        }
        return new string(Buffer, Ridx, Math.Max(0, Widx - Ridx));
    }
}