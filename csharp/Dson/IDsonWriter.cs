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

using Wjybxx.Dson.IO;
using Wjybxx.Dson.Text;
using Wjybxx.Dson.Types;
using Google.Protobuf;

namespace Wjybxx.Dson;

/// <summary>
/// 1. 写数组普通元素的时候，name传null或零值，写嵌套对象时使用无name参数的start方法（实在不想定义太多的方法）
/// 2. double、bool、null由于可以从无符号字符串精确解析得出，因此可以总是不输出类型标签，
/// 3. 内置结构体总是输出类型标签，且总是Flow模式，可以降低使用复杂度；
/// 4. C#由于有值类型，直接使用<see cref="FieldNumber"/>作为name参数，可大幅简化api
/// </summary>
/// <typeparam name="TName">name的类型，string或<see cref="FieldNumber"/></typeparam>
public interface IDsonWriter<in TName> : IDisposable where TName : IEquatable<TName>
{
    void Flush();

    /// <summary>
    /// 获取当前上下文的类型
    /// </summary>
    DsonContextType ContextType { get; }

    /// <summary>
    /// 当前是否处于等待写入name的状态
    /// </summary>
    bool IsAtName { get; }

    /// <summary>
    /// 编码的时候，用户总是习惯 name和value 同时写入，
    /// 但在写Array或Object容器的时候，不能同时完成，需要先写入name再开始写值
    /// </summary>
    /// <param name="name"></param>
    void WriteName(TName name);

    #region 简单值

    /// <summary>
    /// 写入一个int值
    /// </summary>
    /// <param name="name">字段的名字</param>
    /// <param name="value">要写入的值</param>
    /// <param name="wireType">数字的二进制编码类型</param>
    /// <param name="style">数字的文本编码类型；如果为null，则按照简单模式编码</param>
    void WriteInt32(TName name, int value, WireType wireType, INumberStyle? style = null);

    void WriteInt64(TName name, long value, WireType wireType, INumberStyle? style = null);

    void WriteFloat(TName name, float value, INumberStyle? style = null);

    void WriteDouble(TName name, double value, INumberStyle? style = null);

    void WriteBoolean(TName name, bool value);

    void WriteString(TName name, string value, StringStyle style = StringStyle.Auto);

    void WriteNull(TName name);

    void WriteBinary(TName name, DsonBinary dsonBinary);

    void WriteBinary(TName name, int type, DsonChunk chunk);

    void WriteExtInt32(TName name, DsonExtInt32 value, WireType wireType, INumberStyle? style = null);

    void WriteExtInt64(TName name, DsonExtInt64 value, WireType wireType, INumberStyle? style = null);

    void WriteExtDouble(TName name, DsonExtDouble value, INumberStyle? style = null);

    void WriteExtString(TName name, DsonExtString value, StringStyle style = StringStyle.Auto);

    void WriteRef(TName name, ObjectRef objectRef);

    void WriteTimestamp(TName name, OffsetTimestamp timestamp);

    #endregion

    #region 容器

    void WriteStartArray(ObjectStyle style = ObjectStyle.Indent);

    void WriteEndArray();

    void WriteStartObject(ObjectStyle style = ObjectStyle.Indent);

    void WriteEndObject();

    /// <summary>
    /// Header应该保持简单，因此通常应该使用Flow模式
    /// </summary>
    /// <param name="style">文本格式</param>
    void WriteStartHeader(ObjectStyle style = ObjectStyle.Flow);

    void WriteEndHeader();

    /// <summary>
    /// 开始写一个数组
    /// 1.数组内元素没有名字，因此name传 null或0 即可
    /// <code>
    ///    writer.WriteStartArray(name, ObjectStyle.INDENT);
    ///    for (String coderName: coderNames) {
    ///        writer.WriteString(null, coderName);
    ///    }
    ///    writer.WriteEndArray();
    /// </code>
    /// </summary>
    /// <param name="name"></param>
    /// <param name="style"></param>
    void WriteStartArray(TName name, ObjectStyle style = ObjectStyle.Indent) {
        WriteName(name);
        WriteStartArray(style);
    }

    /// <summary>
    /// 开始写一个普通对象
    /// <code>
    ///    writer.WriteStartObject(name, ObjectStyle.INDENT);
    ///    writer.WriteString("name", "wjybxx")
    ///    writer.WriteInt32("age", 28)
    ///    writer.WriteEndObject();
    /// </code>
    /// </summary>
    /// <param name="name"></param>
    /// <param name="style"></param>
    void WriteStartObject(TName name, ObjectStyle style = ObjectStyle.Indent) {
        WriteName(name);
        WriteStartObject(style);
    }

    #endregion

    #region 特殊接口

    /// <summary>
    /// 写入一个简单对象头 -- 仅有一个clsName属性的header。
    /// 仅适用于string键
    /// </summary>
    /// <param name="clsName"></param>
    /// <exception cref="ArgumentNullException"></exception>
    void WriteSimpleHeader(string clsName) {
        if (clsName == null) throw new ArgumentNullException(nameof(clsName));
        IDsonWriter<string> textWrite = (IDsonWriter<string>)this;
        textWrite.WriteStartHeader();
        textWrite.WriteString(DsonHeaders.NamesClassName, clsName, StringStyle.AutoQuote);
        textWrite.WriteEndHeader();
    }

    /// <summary>
    /// 向Writer中写入一个Protobuf的Message
    /// (Message最终会写为Binary)
    /// </summary>
    /// <param name="name">字段名字</param>
    /// <param name="binaryType">对应的二进制子类型</param>
    /// <param name="message">pb消息</param>
    void WriteMessage(TName name, int binaryType, IMessage message);

    /// <summary>
    /// 直接写入一个已编码的字节数组
    /// 1.请确保合法性
    /// 2.支持的类型与读方法相同
    /// 
    /// </summary>
    /// <param name="name">字段名字</param>
    /// <param name="type">DsonType</param>
    /// <param name="data">DsonReader.ReadValueAsBytes 读取的数据</param>
    void WriteValueBytes(TName name, DsonType type, byte[] data);

    /// <summary>
    /// 附近一个数据到当前上下文
    /// </summary>
    /// <param name="userData">用户自定义数据</param>
    /// <returns>旧值</returns>
    object? Attach(object userData);

    /// <summary>
    /// 获取附加到当前上下文的数据
    /// </summary>
    /// <returns></returns>
    object Attachment();

    #endregion
}