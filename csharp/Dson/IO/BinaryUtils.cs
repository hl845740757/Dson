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
using Google.Protobuf;

namespace Dson.IO;

/// <summary>
/// C#10不支持逻辑右移，但这里使用算术右移是等价的
/// </summary>
public static class BinaryUtils
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
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
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void CheckBuffer(int bufferLength, int offset, int length) {
        if ((offset | length | (bufferLength - (offset + length))) < 0) {
            throw new ArgumentException($"Array range is invalid. Buffer.length={bufferLength}, offset={offset}, length={length}");
        }
    }

    public static void CheckBuffer(int bufferLength, int offset) {
        if (offset < 0 || offset > bufferLength) {
            throw new ArgumentException($"Array range is invalid. Buffer.length={bufferLength}, offset={offset}");
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

    #region protobuf decode

    // 由于Protobuf在C#端的接口开放较少，我决定不直接使用CodedInputStream和CodedOutputStream，而是自己实现

    /// <summary>
    /// 计算无符号32位整数(非负数)编码后的长度
    /// </summary>
    /// <param name="value"></param>
    /// <returns>编码长度</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static int ComputeUInt32Size(uint value) {
        return CodedOutputStream.ComputeRawVarint32Size(value);
    }

    /// <summary>
    /// 计算无符号64位整数(非负数)编码后的长度
    /// </summary>
    /// <param name="value"></param>
    /// <returns>编码长度</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static int ComputeUint64Size(ulong value) {
        return CodedOutputStream.ComputeRawVarint64Size(value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static int ReadInt32(byte[] buffer, int pos, out int newPos) {
        ulong rawBits = ReadRawVarint64(buffer, pos, out newPos);
        return (int)rawBits;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static long ReadInt64(byte[] buffer, int pos, out int newPos) {
        ulong rawBits = ReadRawVarint64(buffer, pos, out newPos);
        return (int)rawBits;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static int ReadUint32(byte[] buffer, int pos, out int newPos) {
        return (int)ReadRawVarint64(buffer, pos, out newPos);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static long ReadUint64(byte[] buffer, int pos, out int newPos) {
        return (long)ReadRawVarint64(buffer, pos, out newPos);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static int ReadSint32(byte[] buffer, int pos, out int newPos) {
        ulong rawBits = ReadRawVarint64(buffer, pos, out newPos);
        return DecodeZigZag32((uint)rawBits);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static long ReadSint64(byte[] buffer, int pos, out int newPos) {
        ulong rawBits = ReadRawVarint64(buffer, pos, out newPos);
        return DecodeZigZag64(rawBits);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static int ReadFixed32(byte[] buffer, int pos, out int newPos) {
        uint rawBits = ReadRawFixed32(buffer, pos, out newPos);
        return (int)rawBits;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static long ReadFixed64(byte[] buffer, int pos, out int newPos) {
        ulong rawBits = ReadRawFixed64(buffer, pos, out newPos);
        return (long)rawBits;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static float ReadFloat(byte[] buffer, int pos, out int newPos) {
        uint rawBits = ReadRawFixed32(buffer, pos, out newPos);
        return BitConverter.UInt32BitsToSingle(rawBits);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static double ReadDouble(byte[] buffer, int pos, out int newPos) {
        ulong rawBits = ReadRawFixed64(buffer, pos, out newPos);
        return BitConverter.UInt64BitsToDouble(rawBits);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int DecodeZigZag32(uint n) => (int)(n >> 1) ^ -((int)n & 1);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static long DecodeZigZag64(ulong n) => (long)(n >> 1) ^ -((long)n & 1L);

    /** varint编码不区分int和long，而是固定读取到高位字节为0，因此无需两个方法 */
    private static ulong ReadRawVarint64(byte[] buffer, int pos, out int newPos) {
        int shift = 0;
        ulong r = 0;
        do {
            byte b = buffer[pos++];
            r |= (b & 127UL) << shift; // 取后7位左移
            if (b < 128U) { // 高位0
                newPos = pos;
                return r;
            }
            shift += 7;
        } while (shift < 64);
        // 读取超过10个字节
        throw new DsonIOException("DsonInput encountered a malformed varint.");
    }

    private static uint ReadRawFixed32(byte[] buffer, int pos, out int newPos) {
        uint r = (((buffer[pos] & 0xffU))
                  | ((buffer[pos + 1] & 0xffU) << 8)
                  | ((buffer[pos + 2] & 0xffU) << 16)
                  | ((buffer[pos + 3] & 0xffU) << 24));
        newPos = pos + 4;
        return r;
    }

    private static ulong ReadRawFixed64(byte[] buffer, int pos, out int newPos) {
        ulong r = (((buffer[pos] & 0xffUL))
                   | ((buffer[pos + 1] & 0xffUL) << 8)
                   | ((buffer[pos + 2] & 0xffUL) << 16)
                   | ((buffer[pos + 3] & 0xffUL) << 24)
                   | ((buffer[pos + 4] & 0xffUL) << 32)
                   | ((buffer[pos + 5] & 0xffUL) << 40)
                   | ((buffer[pos + 6] & 0xffUL) << 48)
                   | ((buffer[pos + 7] & 0xffUL) << 56));
        newPos = pos + 8;
        return r;
    }

    #endregion

    #region protobuf encode

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static int WriteInt32(byte[] buffer, int pos, int value) {
        if (value >= 0) {
            return WriteRawVarint32(buffer, pos, (uint)value);
        }
        else {
            return WriteRawVarint64(buffer, pos, (ulong)value);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static int WriteInt64(byte[] buffer, int pos, long value) {
        return WriteRawVarint64(buffer, pos, (ulong)value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static int WriteUint32(byte[] buffer, int pos, int value) {
        return WriteRawVarint32(buffer, pos, (uint)value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static int WriteUint64(byte[] buffer, int pos, long value) {
        return WriteRawVarint64(buffer, pos, (ulong)value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static int WriteSint32(byte[] buffer, int pos, int value) {
        return WriteRawVarint32(buffer, pos, EncodeZigZag32(value));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static int WriteSint64(byte[] buffer, int pos, long value) {
        return WriteRawVarint64(buffer, pos, EncodeZigZag64(value));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static int WriteFixed32(byte[] buffer, int pos, int value) {
        return WriteRawFixed32(buffer, pos, (uint)value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static int WriteFixed64(byte[] buffer, int pos, long value) {
        return WriteRawFixed64(buffer, pos, (ulong)value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static int WriteFloat(byte[] buffer, int pos, float value) {
        return WriteRawFixed32(buffer, pos, BitConverter.SingleToUInt32Bits(value));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static int WriteDouble(byte[] buffer, int pos, double value) {
        return WriteRawFixed64(buffer, pos, BitConverter.DoubleToUInt64Bits(value));
    }

    /// <summary>
    /// 写入一个变长的64位整数，所有的负数都将固定10字节
    /// </summary>
    /// <param name="buffer"></param>
    /// <param name="pos">开始写入的位置</param>
    /// <param name="value">要写入的值</param>
    /// <returns>写入后的新坐标</returns>
    public static int WriteRawVarint64(byte[] buffer, int pos, ulong value) {
        if (value < 128UL) { // 小数值较多的情况下有意义
            buffer[pos] = (byte)value;
            return pos + 1;
        }
        while (true) {
            if (value > 127UL) {
                buffer[pos++] = (byte)((value & 127UL) | 128UL); // 截取后7位，高位补1
                value >>= 7;
            }
            else {
                buffer[pos++] = (byte)value;
                return pos;
            }
        }
    }

    /// <summary>
    /// 注意：该方法只可以在value为非负数的情况下可调用，外部在转换数据类型的时候要注意
    /// </summary>
    private static int WriteRawVarint32(byte[] buffer, int pos, uint value) {
        if (value < 128U) { // 小数值较多的情况下有意义
            buffer[pos] = (byte)value;
            return pos + 1;
        }
        while (true) {
            if (value > 127U) {
                buffer[pos++] = (byte)((value & 127U) | 128U); // 截取后7位，高位补1
                value >>= 7;
            }
            else {
                buffer[pos++] = (byte)value;
                return pos;
            }
        }
    }

    private static int WriteRawFixed32(byte[] buffer, int pos, uint value) {
        buffer[pos] = (byte)value;
        buffer[pos + 1] = (byte)(value >> 8);
        buffer[pos + 2] = (byte)(value >> 16);
        buffer[pos + 3] = (byte)(value >> 24);
        return pos + 4;
    }

    private static int WriteRawFixed64(byte[] buffer, int pos, ulong value) {
        buffer[pos] = (byte)value;
        buffer[pos + 1] = (byte)(value >> 8);
        buffer[pos + 2] = (byte)(value >> 16);
        buffer[pos + 3] = (byte)(value >> 24);
        buffer[pos + 4] = (byte)(value >> 32);
        buffer[pos + 5] = (byte)(value >> 40);
        buffer[pos + 6] = (byte)(value >> 48);
        buffer[pos + 7] = (byte)(value >> 56);
        return pos + 8;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static uint EncodeZigZag32(int n) => (uint)(n << 1 ^ n >> 31);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ulong EncodeZigZag64(long n) => (ulong)(n << 1 ^ n >> 63);

    #endregion
}