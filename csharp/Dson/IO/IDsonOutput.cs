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

using Google.Protobuf;

namespace Dson.IO;

/// <summary>
/// 1. 数字采用小端编码
/// 2. String采用UTF8编码
/// </summary>
public interface IDsonOutput : IDisposable
{
    #region Basic

    void WriteRawByte(byte value);

    void WriteRawByte(int value) {
        WriteRawByte((byte)value);
    }

    void WriteInt32(int value);

    void WriteUint32(int value);

    void WriteSint32(int value);

    void WriteFixed32(int value);

    void WriteInt64(long value);

    void WriteUint64(long value);

    void WriteSint64(long value);

    void WriteFixed64(long value);

    /// <summary>
    /// 该接口固定写入4个字节
    /// </summary>
    /// <param name="value"></param>
    void WriteFloat(float value);

    /// <summary>
    /// 该接口固定写入8个字节
    /// </summary>
    /// <param name="value"></param>
    void WriteDouble(double value);

    /// <summary>
    /// 该接口固定写入一个字节
    /// </summary>
    /// <param name="value"></param>
    void WriteBool(bool value);

    /// <summary>
    /// 该接口先以Uint32格式写入String的长度，再写入String以UTF8编码后的内容
    /// </summary>
    /// <param name="value">要写入的字符串</param>
    void WriteString(string value);

    /// <summary>
    /// 仅写入内容，不会写入数组的长度
    /// </summary>
    /// <param name="value">要写入的字节数组</param>
    void WriteRawBytes(byte[] value) {
        WriteRawBytes(value, 0, value.Length);
    }

    /// <summary>
    /// 仅写入内容，不会写入数组的长度
    /// </summary>
    void WriteRawBytes(byte[] value, int offset, int length);

    /// <summary>
    /// 写入一个Protobuf消息
    /// 1.只写入message的内容部分，不包含长度信息
    /// 2.该方法用于避免创建临时的字节数组
    ///
    /// <code>
    ///    byte[] data = message.toByteArray();
    ///    output.writeRawBytes(data);
    /// </code>
    /// </summary>
    /// <param name="value"></param>
    void WriteMessage(IMessage value);

    #endregion

    #region Advance

    /// <summary>
    /// 当前写索引
    /// </summary>
    int Position { get; set; }

    /// <summary>
    /// 在指定位置写入一个byte
    /// </summary>
    /// <param name="pos">写索引</param>
    /// <param name="value">value</param>
    void SetByte(int pos, byte value);

    /// <summary>
    /// 在指定索引位置以Fixed32格式写入一个int值
    /// 该方法可能有较大的开销，不宜频繁使用
    /// </summary>
    /// <param name="pos">写索引</param>
    /// <param name="value">value</param>
    void SetFixedInt32(int pos, int value);

    void Flush();

    #endregion
}