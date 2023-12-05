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
/// </summary>
public interface IDsonInput : IDisposable
{
    #region Basic

    byte ReadRawByte();

    int ReadUint8() {
        return ReadRawByte() & 0xFF;
    }

    int ReadInt32();

    int ReadUint32();

    int ReadSint32();

    int ReadFixed32();

    long ReadInt64();

    long ReadUint64();

    long ReadSint64();

    long ReadFixed64();

    /// <summary>
    /// 该接口默认读取4字节
    /// </summary>
    /// <returns></returns>
    float ReadFloat();

    /// <summary>
    /// 该接口默认读取8字节
    /// </summary>
    /// <returns></returns>
    double ReadDouble();

    bool ReadBool();

    string ReadString();

    /// <summary>
    /// 读取原始的bytes
    /// </summary>
    /// <param name="size">要读取的字节数</param>
    /// <returns></returns>
    byte[] ReadRawBytes(int size);

    /// <summary>
    /// 跳过指定数量的字节
    /// </summary>
    /// <param name="n">要跳过的字节数</param>
    void SkipRawBytes(int n);

    /// <summary>
    /// 从输入中读取一个protobuf消息
    /// </summary>
    /// <param name="parser">消息的解析器</param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    T ReadMessage<T>(MessageParser<T> parser) where T : IMessage<T>;

    #endregion

    #region Advance

    /// <summary>
    /// 当前读索引位置
    /// </summary>
    int Position { get; set; }

    /// <summary>
    /// 获取指定索引位置的字节
    /// 不会导致读索引变更
    /// </summary>
    /// <param name="pos"></param>
    /// <returns></returns>
    byte GetByte(int pos);

    /// <summary>
    /// 从指定位置读取4个字节为int
    /// 不会导致读索引变更
    /// </summary>
    /// <param name="pos"></param>
    /// <returns></returns>
    int GetFixed32(int pos);

    /// <summary>
    /// 限制接下来可读取的字节数
    /// </summary>
    /// <param name="byteLimit">可用字节数</param>
    /// <returns>前一次设置的限制点</returns>
    int PushLimit(int byteLimit);

    /// <summary>
    /// 恢复限制
    /// </summary>
    /// <param name="oldLimit"></param>
    void PopLimit(int oldLimit);

    /// <summary>
    /// 查询在到达限制之前的可用字节数
    /// </summary>
    /// <returns>剩余可用的字节数</returns>
    int GetBytesUntilLimit();

    /// <summary>
    /// 是否达到输入流的末端
    /// </summary>
    /// <returns>如果到达流的末尾则返回true</returns>
    bool IsAtEnd();

    #endregion
}