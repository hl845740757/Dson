#region LICENSE

//  Copyright 2023 wjybxx
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

using System.Runtime.CompilerServices;

namespace Dson.IO;

/// <summary>
/// C#10不支持逻辑右移，但这里使用算术右移是等价的
/// </summary>
public static class BinaryUtils
{
    public static void CheckBuffer(byte[] buffer, int offset, int length) {
        CheckBuffer(buffer.Length, offset, length);
    }

    /// <summary>
    /// 检查buffer的参数
    /// </summary>
    /// <param name="bufferLength">buffer数组的长度</param>
    /// <param name="offset">数据的起始索引</param>
    /// <param name="length">数据的长度</param>
    /// <exception cref="ArgumentException"></exception>
    public static void CheckBuffer(int bufferLength, int offset, int length) {
        if ((offset | length | (bufferLength - (offset + length))) < 0) {
            throw new ArgumentException($"Array range is invalid. Buffer.length={bufferLength}, offset={offset}, length={length}");
        }
    }

    /** c#的byte默认是无符号的；这一点我觉得C#是对的... */
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int ToUint(byte value) {
        return value;
    }

    #region 大端编码

    public static byte GetByte(byte[] buffer, int index) {
        return buffer[index];
    }

    public static void SetByte(byte[] buffer, int index, int value) {
        buffer[index] = (byte)value;
    }

    public static void SetShort(byte[] buffer, int index, int value) {
        buffer[index] = (byte)(value >> 8);
        buffer[index + 1] = (byte)value;
    }

    public static short GetShort(byte[] buffer, int index) {
        return (short)((buffer[index] << 8)
                       | (buffer[index + 1] & 0xff));
    }

    public static void SetInt(byte[] buffer, int index, int value) {
        buffer[index] = (byte)(value >> 24);
        buffer[index + 1] = (byte)(value >> 16);
        buffer[index + 2] = (byte)(value >> 8);
        buffer[index + 3] = (byte)value;
    }

    public static int GetInt(byte[] buffer, int index) {
        return (((buffer[index] & 0xff) << 24)
                | ((buffer[index + 1] & 0xff) << 16)
                | ((buffer[index + 2] & 0xff) << 8)
                | ((buffer[index + 3] & 0xff)));
    }

    public static void SetLong(byte[] buffer, int index, long value) {
        buffer[index] = (byte)(value >> 56);
        buffer[index + 1] = (byte)(value >> 48);
        buffer[index + 2] = (byte)(value >> 40);
        buffer[index + 3] = (byte)(value >> 32);
        buffer[index + 4] = (byte)(value >> 24);
        buffer[index + 5] = (byte)(value >> 16);
        buffer[index + 6] = (byte)(value >> 8);
        buffer[index + 7] = (byte)value;
    }

    public static long GetLong(byte[] buffer, int index) {
        return (((buffer[index] & 0xffL) << 56)
                | ((buffer[index + 1] & 0xffL) << 48)
                | ((buffer[index + 2] & 0xffL) << 40)
                | ((buffer[index + 3] & 0xffL) << 32)
                | ((buffer[index + 4] & 0xffL) << 24)
                | ((buffer[index + 5] & 0xffL) << 16)
                | ((buffer[index + 6] & 0xffL) << 8)
                | ((buffer[index + 7] & 0xffL)));
    }

    #endregion

    #region 小端编码

    public static void SetShortLe(byte[] buffer, int index, int value) {
        buffer[index] = (byte)value;
        buffer[index + 1] = (byte)(value >> 8);
    }

    public static short GetShortLe(byte[] buffer, int index) {
        return (short)((buffer[index] & 0xff)
                       | (buffer[index + 1] << 8));
    }

    public static void SetIntLe(byte[] buffer, int index, int value) {
        buffer[index] = (byte)value;
        buffer[index + 1] = (byte)(value >> 8);
        buffer[index + 2] = (byte)(value >> 16);
        buffer[index + 3] = (byte)(value >> 24);
    }

    public static int GetIntLe(byte[] buffer, int index) {
        return (((buffer[index] & 0xff))
                | ((buffer[index + 1] & 0xff) << 8)
                | ((buffer[index + 2] & 0xff) << 16)
                | ((buffer[index + 3] & 0xff) << 24));
    }

    public static void SetLongLe(byte[] buffer, int index, long value) {
        buffer[index] = (byte)value;
        buffer[index + 1] = (byte)(value >> 8);
        buffer[index + 2] = (byte)(value >> 16);
        buffer[index + 3] = (byte)(value >> 24);
        buffer[index + 4] = (byte)(value >> 32);
        buffer[index + 5] = (byte)(value >> 40);
        buffer[index + 6] = (byte)(value >> 48);
        buffer[index + 7] = (byte)(value >> 56);
    }

    public static long GetLongLe(byte[] buffer, int index) {
        return (((buffer[index] & 0xffL))
                | ((buffer[index + 1] & 0xffL) << 8)
                | ((buffer[index + 2] & 0xffL) << 16)
                | ((buffer[index + 3] & 0xffL) << 24)
                | ((buffer[index + 4] & 0xffL) << 32)
                | ((buffer[index + 5] & 0xffL) << 40)
                | ((buffer[index + 6] & 0xffL) << 48)
                | ((buffer[index + 7] & 0xffL) << 56));
    }

    #endregion
}